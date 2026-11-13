using System.Security.Claims;
using KnowledgeAssistant.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/usage")]
[Authorize]
public class UsageController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsageController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var orgId = int.Parse(User.FindFirstValue("org_id") ?? "0");
        var usage = await _db.UsageCounters.FirstOrDefaultAsync(u => u.OrganizationId == orgId);

        return Ok(new
        {
            asked = usage?.QuestionsAsked ?? 0,
            answered = usage?.QuestionsAnswered ?? 0,
            unanswered = usage?.QuestionsUnanswered ?? 0
        });
    }
}