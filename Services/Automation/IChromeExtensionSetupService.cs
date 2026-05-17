namespace ShoppingAgent.Services.Automation;

public interface IChromeExtensionSetupService
{
    Task<string> OpenInstallInstructionsAsync();
}
