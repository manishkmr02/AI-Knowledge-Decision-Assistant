using KnowledgeAssistant.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly AppDbContext _db;
    public AssistantController(AppDbContext db) => _db = db;

    // Public: frontend uses this to render the assistant header and validate the token
    // GET /api/assistant/info?token=abc123
    [HttpGet("info")]
    public async Task<IActionResult> Info([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Missing assistant token." });

        var org = await _db.Organizations
            .Where(o => o.AssistantToken == token)
            .Select(o => new { o.Id, o.Name })
            .FirstOrDefaultAsync();

        if (org is null)
            return NotFound(new { error = "Assistant not found. Please check the link." });

        return Ok(new { organizationName = org.Name, assistantToken = token });
    }
}