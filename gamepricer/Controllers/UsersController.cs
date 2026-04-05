using System.Security.Claims;
using gamepricer.Data;
using gamepricer.Dtos.Users;
using gamepricer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamepricer.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPut("{userId:guid}")]
        public async Task<ActionResult> UpdateUser(Guid userId, UpdateUserRequestDto request)
        {
            var currentUserId = GetUserId();
            if (currentUserId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            if (currentUserId != userId)
                return Forbid();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Trim() != user.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u =>
                    !u.IsDeleted && u.Email == request.Email.Trim() && u.Id != userId);

                if (emailExists)
                    return BadRequest(new { message = "Bu e-posta başka bir kullanıcı tarafından kullanılıyor." });

                user.Email = request.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Username) && request.Username.Trim() != user.Username)
            {
                var usernameExists = await _context.Users.AnyAsync(u =>
                    !u.IsDeleted && u.Username == request.Username.Trim() && u.Id != userId);

                if (usernameExists)
                    return BadRequest(new { message = "Bu kullanıcı adı başka bir kullanıcı tarafından kullanılıyor." });

                user.Username = request.Username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = PasswordHelper.HashPassword(request.Password);
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Kullanıcı bilgileri güncellendi.",
                userId = user.Id,
                username = user.Username,
                email = user.Email
            });
        }

        [HttpDelete("{userId:guid}")]
        public async Task<ActionResult> DeleteUser(Guid userId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == Guid.Empty)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            if (currentUserId != userId)
                return Forbid();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;

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