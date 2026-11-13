using System.Security.Claims;
using KnowledgeAssistant.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/assistant/admin")]
[Authorize]
public class AssistantAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AssistantAdminController(AppDbContext db) => _db = db;

    private int OrgId => int.Parse(User.FindFirstValue("org_id") ?? "0");

    // GET /api/assistant/admin/info
    [HttpGet("info")]
    public async Task<IActionResult> Info()
    {
        var org = await _db.Organizations.FindAsync(OrgId);
        if (org is null) return Unauthorized();
        return Ok(new { assistantToken = org.AssistantToken });
    }

    // POST /api/assistant/admin/regenerate
    [HttpPost("regenerate")]
    public async Task<IActionResult> Regenerate()
    {
        var org = await _db.Organizations.FindAsync(OrgId);
        if (org is null) return Unauthorized();

        org.AssistantToken = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync();

        return Ok(new { assistantToken = org.AssistantToken });
    }
}