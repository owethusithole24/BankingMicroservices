using AuthService.Data;
using AuthService.Models;
using AuthService.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

/// <summary>Login endpoint that issues JWT tokens.</summary>
[ApiController]
[Route("api/[controller]")] // -> /api/auth
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthDbContext db, JwtTokenService tokens, ILogger<AuthController> logger)
    {
        _db = db;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>POST /api/auth/login — verify credentials and return a JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var hash = PasswordHasher.Hash(req.Password);
        var user = await _db.Customers
            .FirstOrDefaultAsync(c => c.Username == req.Username && c.PasswordHash == hash);

        if (user is null)
        {
            // A failed login is a security-relevant event — logged for the future SOC layer (§9).
            _logger.LogWarning("Failed login attempt for username {Username}", req.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var (token, expiresAt) = _tokens.CreateToken(user.Username, user.Role);
        _logger.LogInformation("User {Username} logged in successfully (role {Role})", user.Username, user.Role);

        return Ok(new
        {
            token,
            expiresAt,
            username = user.Username,
            role = user.Role
        });
    }
}
