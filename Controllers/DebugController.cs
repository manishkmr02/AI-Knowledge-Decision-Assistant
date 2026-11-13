using System.Security.Claims;
using KnowledgeAssistant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/debug")]
[Authorize]
public class DebugController : ControllerBase
{
    private readonly RetrievalService _retrieval;
    public DebugController(RetrievalService retrieval) => _retrieval = retrieval;

    private int OrgId => int.Parse(User.FindFirstValue("org_id") ?? "0");

    // GET /api/debug/search?q=scholarship%20eligibility
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int k = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Provide ?q=<question>." });

        var results = await _retrieval.SearchAsync(OrgId, q, k);

        return Ok(new
        {
            query = q,
            organizationId = OrgId,
            count = results.Count,
            results = results.Select(r => new
            {
                r.Score,
                r.FileName,
                r.PageNumber,
                Text = r.Text.Length > 400 ? r.Text[..400] + "…" : r.Text,
                Citation = $"{r.FileName} — Page {r.PageNumber}"
            })
        });
    }
}