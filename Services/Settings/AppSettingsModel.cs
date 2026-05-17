namespace ShoppingAgent.Services.Settings;

public sealed class AppSettingsModel
{
    public string OpenAiApiKey { get; set; } = "";
    public string OpenAiModel { get; set; } = "gpt-4.1-mini";
    public decimal MonthlyBudgetUsd { get; set; } = 10;
    public decimal DailyBudgetUsd { get; set; } = 1;
    public int MaxTokensPerRequest { get; set; } = 2500;
    public int MaxAiRetries { get; set; } = 2;
    public bool EnableAiBrowserFallback { get; set; }
    public bool EnableShoppingAutomation { get; set; }
    public string TescoEmail { get; set; } = "";
    public string TescoPassword { get; set; } = "";
}
