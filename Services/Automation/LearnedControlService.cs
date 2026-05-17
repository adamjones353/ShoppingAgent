using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Contracts;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services.Automation;

public sealed class LearnedControlService(IDbContextFactory<AppDbContext> dbFactory) : ILearnedControlService
{
    public async Task<List<LearnedBrowserControl>> GetControlsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.LearnedBrowserControls
            .OrderBy(x => x.SiteName)
            .ThenBy(x => x.Purpose)
            .ThenByDescending(x => x.ConfidenceScore)
            .ToListAsync();
    }

    public async Task<List<LearnedBrowserControl>> FindCandidatesAsync(string siteName, string purpose, string url)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.LearnedBrowserControls
            .Where(x => x.SiteName == siteName && x.Purpose == purpose && x.ConfidenceScore >= 0.7)
            .OrderByDescending(x => x.ConfidenceScore)
            .Take(5)
            .ToListAsync();
    }

    public async Task SaveSuccessAsync(LearnedControlRequest request, double confidence)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.LearnedBrowserControls.Add(new LearnedBrowserControl
        {
            SiteName = request.SiteName,
            PageType = request.PageType,
            Purpose = request.Purpose,
            LocatorType = request.LocatorType,
            LocatorValue = request.LocatorValue,
            AccessibleRole = request.AccessibleRole,
            AccessibleName = request.AccessibleName,
            UrlPattern = request.UrlPattern,
            ConfidenceScore = Math.Clamp(confidence, 0, 1),
            LastSuccessfulUse = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task MarkSuccessAsync(int controlId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var control = await db.LearnedBrowserControls.FirstAsync(x => x.Id == controlId);
        control.ConfidenceScore = Math.Min(1, control.ConfidenceScore + 0.08);
        control.LastSuccessfulUse = DateTimeOffset.UtcNow;
        control.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task MarkFailureAsync(int controlId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var control = await db.LearnedBrowserControls.FirstAsync(x => x.Id == controlId);
        control.FailureCount++;
        control.ConfidenceScore = Math.Max(0, control.ConfidenceScore - 0.12);
        control.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
