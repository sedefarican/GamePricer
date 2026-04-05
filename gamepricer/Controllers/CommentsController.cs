using System.Security.Claims;
using gamepricer.Data;
using gamepricer.Dtos.Comments;
using gamepricer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamepricer.Controllers
{
    [ApiController]
    [Route("comments")]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPut("{commentId:guid}")]
        [Authorize]
        public async Task<ActionResult<CommentItemDto>> UpdateComment(Guid commentId, UpdateCommentRequestDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                return NotFound(new { message = "Yorum bulunamadı." });

            if (comment.UserId != userId)
                return Forbid();

            comment.Content = request.Content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new CommentItemDto
            {
                Id = comment.Id,
                GameId = comment.GameId,
                UserId = comment.UserId,
                Username = comment.User.Username,
                Content = comment.Content,
                LikeCount = comment.Likes.Count,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };

            return Ok(result);
        }

        [HttpDelete("{commentId:guid}")]
        [Authorize]
        public async Task<ActionResult> DeleteComment(Guid commentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                return NotFound(new { message = "Yorum bulunamadı." });

            if (comment.UserId != userId)
                return Forbid();

            comment.IsDeleted = true;
            comment.DeletedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{commentId:guid}/like")]
        [Authorize]
        public async Task<ActionResult> LikeComment(Guid commentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                return NotFound(new { message = "Yorum bulunamadı." });

            var alreadyLiked = await _context.CommentLikes
                .AnyAsync(cl => cl.UserId == userId && cl.CommentId == commentId);

            if (alreadyLiked)
                return BadRequest(new { message = "Bu yorumu zaten beğendin." });

            var like = new CommentLike
            {
                UserId = userId,
                CommentId = commentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommentLikes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Yorum beğenildi." });
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}