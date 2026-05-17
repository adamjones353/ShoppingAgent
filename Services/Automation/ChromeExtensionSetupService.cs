using System.Diagnostics;

namespace ShoppingAgent.Services.Automation;

public sealed class ChromeExtensionSetupService : IChromeExtensionSetupService
{
    public Task<string> OpenInstallInstructionsAsync()
    {
        var extensionPath = Path.Combine(AppContext.BaseDirectory, "BrowserExtension", "ShoppingAgentTesco");
        if (!Directory.Exists(extensionPath))
        {
            return Task.FromResult($"Extension folder not found: {extensionPath}. Build the app first.");
        }

        Process.Start(new ProcessStartInfo { FileName = extensionPath, UseShellExecute = true });
        Process.Start(new ProcessStartInfo { FileName = "chrome://extensions", UseShellExecute = true });
        return Task.FromResult($"Opened Chrome Extensions and the extension folder. Enable Developer mode, click Load unpacked, and choose:\n{extensionPath}");
    }
}
