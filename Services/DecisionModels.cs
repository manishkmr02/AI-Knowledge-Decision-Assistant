namespace KnowledgeAssistant.Services;

public record DecisionResult(string Type, string Question, string Result, string Reasoning);

public static class DecisionModels
{
    // 1. BOOLEAN: Eligibility
    public static DecisionResult EvaluateEligibility(string condition, bool isMet, string reason)
        => new("Boolean", condition, isMet ? "YES" : "NO", reason);

    // 2. CHOICE: Category
    public static DecisionResult EvaluateCategory(string[] options, string chosen, string reason)
        => new("Choice", $"Which of these applies: {string.Join(", ", options)}?", chosen, reason);

    // 3. SCORE: Strength
    public static DecisionResult EvaluateScore(string criteria, int score, string reason)
        => new("Score", criteria, $"{score}/10", reason);
}