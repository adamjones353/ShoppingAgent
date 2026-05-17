namespace ShoppingAgent.Services.Settings;

public interface ISettingsService
{
    Task<AppSettingsModel> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettingsModel settings);
}
