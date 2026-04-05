using System.Security.Claims;
using gamepricer.Data;
using gamepricer.Dtos.Comments;
using gamepricer.Dtos.Games;
using gamepricer.Dtos.Itad;
using gamepricer.Entities;
using gamepricer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamepricer.Controllers
{



    [ApiController]
    [Route("games")]
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IItadApiClient _itad;
        private readonly GameLiveDataService _live;

        public GamesController(AppDbContext context, IItadApiClient itad, GameLiveDataService live)
        {
            _context = context;
            _itad = itad;
            _live = live;
        }

        [HttpGet("{gameId:guid}/comments")]
        public async Task<ActionResult<IEnumerable<CommentItemDto>>> GetGameComments(Guid gameId)
        {
            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists)
                return NotFound(new { message = "Oyun bulunamadı." });

            var comments = await _context.Comments
                .Where(c => c.GameId == gameId && !c.IsDeleted)
                .Include(c => c.User)
                .Include(c => c.Likes)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentItemDto
                {
                    Id = c.Id,
                    GameId = c.GameId,
                    UserId = c.UserId,
                    Username = c.User.Username,
                    Content = c.Content,
                    LikeCount = c.Likes.Count,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost("{gameId:guid}/comments")]
        [Authorize]
        public async Task<ActionResult<CommentItemDto>> CreateComment(Guid gameId, CreateCommentRequestDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists)
                return NotFound(new { message = "Oyun bulunamadı." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                UserId = userId,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = new CommentItemDto
            {
                Id = comment.Id,
                GameId = comment.GameId,
                UserId = comment.UserId,
                Username = user.Username,
                Content = comment.Content,
                LikeCount = 0,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };

            return StatusCode(201, result);
        }

        [HttpGet("popular")]
        public async Task<ActionResult<IEnumerable<GameListItemDto>>> GetPopularGames(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] bool live = true,
            [FromQuery] string? country = null,
            CancellationToken cancellationToken = default)
        {
            var games = await _context.Games
                .Include(g => g.Prices)
                .OrderByDescending(g => g.Favorites.Count + g.Comments.Count)
                .ThenBy(g => g.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(g => new GameListItemDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    CoverImageUrl = g.CoverImageUrl,
                    LowestPrice = g.Prices
                        .Where(p => p.IsAvailable)
                        .OrderBy(p => p.Price)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            if (live && games.Count > 0)
            {
                try
                {
                    await _live.EnrichListItemsAsync(games, country, cancellationToken);
                }
                catch
                {
                    /* ITAD yoksa veya hata: veritabanı verisiyle dön */
                }
            }

            return Ok(games);
        }

        [HttpGet("{gameId:guid}")]
        public async Task<ActionResult<GameDetailDto>> GetGameById(
            Guid gameId,
            [FromQuery] bool live = true,
            CancellationToken cancellationToken = default)
        {
            var game = await _context.Games
                .Include(g => g.Prices)
                    .ThenInclude(p => p.Platform)
                .Include(g => g.GameCategories)
                    .ThenInclude(gc => gc.Category)
                .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

            if (game == null)
                return NotFound(new { message = "Oyun bulunamadı." });

            var result = new GameDetailDto
            {
                Id = game.Id,
                Name = game.Name,
                Slug = game.Slug,
                Description = game.Description,
                Developer = game.Developer,
                Publisher = game.Publisher,
                CoverImageUrl = game.CoverImageUrl,
                Categories = game.GameCategories.Select(gc => gc.Category.Name).ToList(),
                Prices = game.Prices
                    .Where(p => p.IsAvailable && (p.Price > 0 || (p.DiscountRate ?? 0) > 0))
                    .Select(p => new GamePriceDto
                    {
                        PlatformName = p.Platform.Name,
                        Price = p.Price,
                        Currency = p.Currency,
                        DiscountRate = p.DiscountRate,
                        ProductUrl = p.ProductUrl
                    }).ToList()
            };

            if (live)
            {
                try
                {
                    await _live.EnrichDetailCoverAsync(result, cancellationToken);
                }
                catch
                {
                    /* Kapak ITAD olmadan da dönsün */
                }
            }

            return Ok(result);
        }

        /// <summary>Veritabanındaki oyun adıyla ITAD'de eşleştirip güncel mağaza fiyatlarını döndürür.</summary>
        [HttpGet("{gameId:guid}/itad-prices")]
        public async Task<ActionResult<GameItadPricesResponseDto>> GetItadPricesForGame(
            Guid gameId,
            [FromQuery] string? country = null,
            CancellationToken cancellationToken = default)
        {
            var game = await _context.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
            if (game == null)
                return NotFound(new { message = "Oyun bulunamadı." });

            try
            {
                var resolved = await _itad.ResolveGameByTitleAsync(game.Name, cancellationToken);
                if (resolved == null)
                    return NotFound(new { message = "Bu oyun IsThereAnyDeal kataloğunda bulunamadı." });

                var blocks = await _itad.GetPricesForGamesAsync(
                    new[] { resolved.Id },
                    country ?? string.Empty,
                    cancellationToken);
                var deals = blocks.FirstOrDefault(b => b.ItadGameId == resolved.Id)?.Deals
                    ?? blocks.FirstOrDefault()?.Deals
                    ?? Array.Empty<ItadDealOfferDto>();

                return Ok(new GameItadPricesResponseDto
                {
                    GameId = gameId,
                    MatchedTitle = resolved.Title,
                    ItadGameId = resolved.Id,
                    Deals = deals.ToList()
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { message = "ITAD API'ye ulaşılamadı.", detail = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<GameListItemDto>>> SearchGames(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] bool live = true,
            [FromQuery] string? country = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Arama metni boş olamaz." });

            var term = q.Trim();
            var games = await _context.Games
                .Include(g => g.Prices)
                .Where(g => g.Name.Contains(term))
                .OrderBy(g => g.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(g => new GameListItemDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    CoverImageUrl = g.CoverImageUrl,
                    LowestPrice = g.Prices
                        .Where(p => p.IsAvailable)
                        .OrderBy(p => p.Price)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            if (live && games.Count > 0)
            {
                try
                {
                    await _live.EnrichListItemsAsync(games, country, cancellationToken);
                }
                catch
                {
                    /* ITAD hatasında yerel sonuçlar */
                }
            }

            return Ok(games);
        }


        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}