using System.Security.Claims;
using gamepricer.Data;
using gamepricer.Dtos.Favorites;
using gamepricer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamepricer.Controllers
{
    [ApiController]
    [Route("favorites")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> AddFavorite(AddFavoriteRequestDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var gameExists = await _context.Games.AnyAsync(g => g.Id == request.GameId);
            if (!gameExists)
                return NotFound(new { message = "Oyun bulunamadı." });

            var alreadyExists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.GameId == request.GameId);

            if (alreadyExists)
                return BadRequest(new { message = "Bu oyun zaten favorilerde." });

            var favorite = new Favorite
            {
                UserId = userId,
                GameId = request.GameId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Oyun favorilere eklendi." });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FavoriteItemDto>>> GetFavorites()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Game)
                    .ThenInclude(g => g.Prices)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FavoriteItemDto
                {
                    GameId = f.GameId,
                    GameName = f.Game.Name,
                    CoverImageUrl = f.Game.CoverImageUrl,
                    LowestPrice = f.Game.Prices
                        .Where(p => p.IsAvailable)
                        .OrderBy(p => p.Price)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefault(),
                    AddedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(favorites);
        }

        [HttpDelete("{gameId:guid}")]
        public async Task<ActionResult> RemoveFavorite(Guid gameId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.GameId == gameId);

            if (favorite == null)
                return NotFound(new { message = "Favori kaydı bulunamadı." });

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}