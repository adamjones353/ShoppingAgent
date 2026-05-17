using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services.Settings;

public sealed class SettingsService(IDbContextFactory<AppDbContext> dbFactory) : ISettingsService
{
    public async Task<AppSettingsModel> GetSettingsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var values = await db.AppSettings.ToDictionaryAsync(x => x.Key, x => x.Value);
        return new AppSettingsModel
        {
            OpenAiApiKey = Get(values, "OpenAi:ApiKey", ""),
            OpenAiModel = Get(values, "OpenAi:Model", "gpt-4.1-mini"),
            MonthlyBudgetUsd = Decimal(values, "OpenAi:MonthlyBudgetUsd", 10),
            DailyBudgetUsd = Decimal(values, "OpenAi:DailyBudgetUsd", 1),
            MaxTokensPerRequest = Int(values, "OpenAi:MaxTokensPerRequest", 2500),
            MaxAiRetries = Int(values, "OpenAi:MaxRetries", 2),
            EnableAiBrowserFallback = Bool(values, "Automation:EnableAiBrowserFallback", false),
            EnableShoppingAutomation = Bool(values, "Automation:EnableShoppingAutomation", false),
            TescoEmail = Get(values, "Tesco:Email", ""),
            TescoPassword = Get(values, "Tesco:Password", "")
        };
    }

    public async Task SaveSettingsAsync(AppSettingsModel settings)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await Set(db, "OpenAi:ApiKey", settings.OpenAiApiKey);
        await Set(db, "OpenAi:Model", settings.OpenAiModel);
        await Set(db, "OpenAi:MonthlyBudgetUsd", settings.MonthlyBudgetUsd.ToString(CultureInfo.InvariantCulture));
        await Set(db, "OpenAi:DailyBudgetUsd", settings.DailyBudgetUsd.ToString(CultureInfo.InvariantCulture));
        await Set(db, "OpenAi:MaxTokensPerRequest", settings.MaxTokensPerRequest.ToString(CultureInfo.InvariantCulture));
        await Set(db, "OpenAi:MaxRetries", settings.MaxAiRetries.ToString(CultureInfo.InvariantCulture));
        await Set(db, "Automation:EnableAiBrowserFallback", settings.EnableAiBrowserFallback.ToString());
        await Set(db, "Automation:EnableShoppingAutomation", settings.EnableShoppingAutomation.ToString());
        await Set(db, "Tesco:Email", settings.TescoEmail);
        await Set(db, "Tesco:Password", settings.TescoPassword);
        await db.SaveChangesAsync();
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback) => values.TryGetValue(key, out var value) ? value : fallback;
    private static int Int(Dictionary<string, string> values, string key, int fallback) => int.TryParse(Get(values, key, ""), out var value) ? value : fallback;
    private static decimal Decimal(Dictionary<string, string> values, string key, decimal fallback) => decimal.TryParse(Get(values, key, ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    private static bool Bool(Dictionary<string, string> values, string key, bool fallback) => bool.TryParse(Get(values, key, ""), out var value) ? value : fallback;

    private static async Task Set(AppDbContext db, string key, string value)
    {
        var setting = await db.AppSettings.FindAsync(key);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
    }
}
