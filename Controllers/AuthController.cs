using System.Security.Claims;
using KnowledgeAssistant.Data;
using KnowledgeAssistant.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(req.OrganizationName))
            return BadRequest(new { error = "Organization name is required." });
        if (!email.Contains('@') || email.Length < 5)
            return BadRequest(new { error = "A valid email is required." });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { error = "An account with this email already exists." });

        var org = new Organization { Name = req.OrganizationName.Trim() };
        var user = new User { Email = email, Organization = org };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Organizations.Add(org);
        _db.Users.Add(user);
        _db.UsageCounters.Add(new UsageCounter { Organization = org });
        await _db.SaveChangesAsync();

        await SignInAsync(user);
        return Ok(new MeResponse(user.Id, org.Id, org.Name, user.Email, org.AssistantToken));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            return Unauthorized(new { error = "Invalid email or password." });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "");
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid email or password." });

        await SignInAsync(user);
        return Ok(new MeResponse(user.Id, user.OrganizationId,
            user.Organization.Name, user.Email, user.Organization.AssistantToken));
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { ok = true });
    }

    // GET /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _db.Users.Include(u => u.Organization)
                                  .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        return Ok(new MeResponse(user.Id, user.OrganizationId,
            user.Organization.Name, user.Email, user.Organization.AssistantToken));
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("org_id", user.OrganizationId.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}