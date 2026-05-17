using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;
using ShoppingAgent.Services.Settings;

namespace ShoppingAgent.Services.Ai;

public sealed class AiUsageService(IDbContextFactory<AppDbContext> dbFactory, ISettingsService settingsService) : IAiUsageService
{
    public async Task EnsureBudgetAsync(string purpose, int estimatedInputTokens, int estimatedOutputTokens)
    {
        var settings = await settingsService.GetSettingsAsync();
        var estimatedCost = EstimateCostUsd(settings.OpenAiModel, estimatedInputTokens, estimatedOutputTokens);
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var succeededUsage = await db.AiUsageLogs
            .Where(x => x.Succeeded)
            .Select(x => new { x.Model, x.EstimatedInputTokens, x.EstimatedOutputTokens, x.CreatedAt })
            .ToListAsync();

        var dayUsage = succeededUsage.Where(x => x.CreatedAt >= dayStart);
        var monthUsage = succeededUsage.Where(x => x.CreatedAt >= monthStart);

        var dayCost = dayUsage.Sum(x => EstimateCostUsd(x.Model, x.EstimatedInputTokens, x.EstimatedOutputTokens));
        var monthCost = monthUsage.Sum(x => EstimateCostUsd(x.Model, x.EstimatedInputTokens, x.EstimatedOutputTokens));

        if (dayCost + estimatedCost > settings.DailyBudgetUsd)
        {
            throw new InvalidOperationException($"AI daily budget would be exceeded by {purpose}.");
        }

        if (monthCost + estimatedCost > settings.MonthlyBudgetUsd)
        {
            throw new InvalidOperationException($"AI monthly budget would be exceeded by {purpose}.");
        }
    }

    public async Task LogAsync(string purpose, string model, int estimatedInputTokens, int estimatedOutputTokens, bool succeeded, string error = "")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.AiUsageLogs.Add(new AiUsageLog
        {
            Purpose = purpose,
            Model = model,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            Succeeded = succeeded,
            Error = error
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<AiUsageLog>> GetRecentLogsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var logs = await db.AiUsageLogs.ToListAsync();
        return logs
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToList();
    }

    public decimal EstimateCostUsd(string model, int inputTokens, int outputTokens) => (inputTokens + outputTokens) / 1000m * 0.001m;
}
