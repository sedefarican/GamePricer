using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gamepricer.Data;
using gamepricer.Dtos.Auth;
using gamepricer.Entities;
using gamepricer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace gamepricer.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
        {
            bool emailExists = await _context.Users.AnyAsync(u => !u.IsDeleted && u.Email == request.Email.Trim());
            if (emailExists)
                return BadRequest(new { message = "Bu e-posta ile kayıtlı kullanıcı zaten var." });

            bool usernameExists = await _context.Users.AnyAsync(u => !u.IsDeleted && u.Username == request.Username.Trim());
            if (usernameExists)
                return BadRequest(new { message = "Bu kullanıcı adı zaten kullanılıyor." });

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = CreateAuthResponse(user);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    !u.IsDeleted &&
                    (u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername));

            if (user == null)
                return Unauthorized(new { message = "Kullanıcı adı/e-posta veya şifre hatalı." });

            bool isPasswordValid = PasswordHelper.VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Kullanıcı adı/e-posta veya şifre hatalı." });

            var response = CreateAuthResponse(user);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public ActionResult Logout()
        {
            return Ok(new { message = "Çıkış başarılı. İstemci tarafında token silinmelidir." });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == request.Email);

            if (user == null)
            {
                return Ok(new
                {
                    message = "Eğer bu e-posta sistemde kayıtlıysa, şifre sıfırlama işlemi başlatılmıştır."
                });
            }

            var resetToken = Guid.NewGuid().ToString("N");

            return Ok(new
            {
                message = "Şifre sıfırlama bağlantısı simüle edildi.",
                resetToken = resetToken,
                email = user.Email
            });
        }

        private AuthResponseDto CreateAuthResponse(User user)
        {
            string key = _configuration["Jwt:Key"]!;
            string issuer = _configuration["Jwt:Issuer"]!;
            string audience = _configuration["Jwt:Audience"]!;
            int expiresInMinutes = int.Parse(_configuration["Jwt:ExpiresInMinutes"]!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(expiresInMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponseDto
            {
                Token = tokenValue,
                ExpiresAtUtc = expires,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            };
        }
    }
}