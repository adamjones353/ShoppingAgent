using ShoppingAgent.Domain;

namespace ShoppingAgent.Services.Ai;

public interface IAiUsageService
{
    Task EnsureBudgetAsync(string purpose, int estimatedInputTokens, int estimatedOutputTokens);
    Task LogAsync(string purpose, string model, int estimatedInputTokens, int estimatedOutputTokens, bool succeeded, string error = "");
    Task<List<AiUsageLog>> GetRecentLogsAsync();
    decimal EstimateCostUsd(string model, int inputTokens, int outputTokens);
}
