namespace ShoppingAgent.Services.Automation;

public interface IPlaywrightSetupService
{
    Task<string> InstallChromiumAsync();
}
