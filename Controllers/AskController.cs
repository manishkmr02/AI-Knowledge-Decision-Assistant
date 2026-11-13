using KnowledgeAssistant.Data;
using KnowledgeAssistant.Models;
using KnowledgeAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/ask")]
public class AskController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RetrievalService _retrieval;
    private readonly GeminiService _llm;

    public AskController(AppDbContext db, RetrievalService retrieval, GeminiService llm)
    {
        _db = db; _retrieval = retrieval; _llm = llm;
    }

    public record AskRequest(string AssistantToken, string Question);
    public record ReferenceDto(string FileName, int PageNumber, string Snippet);
    public record AskResponse(string Answer, List<ReferenceDto> References);

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Question))
            return BadRequest(new { error = "Question cannot be empty." });

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.AssistantToken == req.AssistantToken);
        if (org is null) return Unauthorized(new { error = "Invalid assistant token." });

        var cleanQ = req.Question.Trim().ToLower();
        if (cleanQ == "hi" || cleanQ == "hii" || cleanQ == "hello" || cleanQ == "hey")
        {
            await IncrementUsage(org.Id, answered: true);
            return Ok(new AskResponse("Hello! How can I help you today with the uploaded documents?", new List<ReferenceDto>()));
        }

        var rawChunks = await _retrieval.SearchAsync(org.Id, req.Question, topK: 8);
        var chunks = rawChunks
            .GroupBy(c => new { c.FileName, c.PageNumber, c.Text })
            .Select(g => g.First())
            .Take(4)
            .ToList();

        if (chunks.Count == 0 || chunks.Max(c => c.Score) < 0.40)
        {
            await IncrementUsage(org.Id, answered: false);
            return Ok(new AskResponse("I couldn't find the answer to that in the uploaded documents.", new List<ReferenceDto>()));
        }

        var contextText = string.Join("\n\n---\n\n", chunks.Select(c => $"[Source: {c.FileName}, Page {c.PageNumber}]\n{c.Text}"));

        try
        {
            // --- 1. LLM DECOMPOSITION ---
            var prompt1 = $@"You are an AI decomposer. Read the Context and the User Question.
Identify if the question requires evaluating policy rules (Boolean), categorizing the question (Choice), or scoring eligibility (Score).
Output ONLY a JSON array. Do not include markdown formatting or explanations.
Example:
[
  {{ ""Type"": ""Boolean"", ""Question"": ""Is the student in Regular mode?"", ""Result"": ""YES"", ""Reasoning"": ""Context states only regular mode is eligible"" }},
  {{ ""Type"": ""Score"", ""Question"": ""How strong is the merit?"", ""Result"": ""8/10"", ""Reasoning"": ""High GPA mentioned"" }}
]
If no structured decisions are needed, output an empty array [].

Context: {contextText}
User Question: {req.Question}";

            var decisionJson = await _llm.GenerateContentAsync(prompt1, jsonMode: true);

            // --- 2. SYSTEM 1 MODELS (Simulated in C#) ---
            // In a full production app, we would deserialize decisionJson into our DecisionModels.cs classes.
            // For this assignment, we pass the structured JSON results directly to the final prompt.
            var system1Results = $"[System 1 Engine Output]\n{decisionJson}";

            // --- 3. FINAL ANSWER GENERATION ---
            var prompt2 = $@"You are a helpful assistant. Answer the User Question using ONLY the provided Context.
You have also been provided with structured decisions made by a System 1 engine.
Use those decisions to build a precise, logical answer. 
Cite sources like (FileName - Page X) where applicable.

System 1 Decisions:
{system1Results}

Context:
{contextText}

User Question: {req.Question}";

            var finalAnswer = await _llm.GenerateContentAsync(prompt2);

            await IncrementUsage(org.Id, answered: true);

            var references = chunks.Select(c => new ReferenceDto(
                c.FileName,
                c.PageNumber,
                c.Text.Length > 100 ? c.Text[..100] + "..." : c.Text
            )).ToList();

            return Ok(new AskResponse(finalAnswer.Trim(), references));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI processing failed: " + ex.Message });
        }
    }

    private async Task IncrementUsage(int orgId, bool answered)
    {
        var usage = await _db.UsageCounters.FirstOrDefaultAsync(u => u.OrganizationId == orgId);
        if (usage == null)
        {
            usage = new UsageCounter { OrganizationId = orgId };
            _db.UsageCounters.Add(usage);
        }
        usage.QuestionsAsked++;
        if (answered) usage.QuestionsAnswered++;
        else usage.QuestionsUnanswered++;
        await _db.SaveChangesAsync();
    }
}