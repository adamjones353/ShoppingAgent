using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace ShoppingAgent.Services.Automation;

public sealed class PlaywrightSetupService : IPlaywrightSetupService
{
    public async Task<string> InstallChromiumAsync()
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "playwright.ps1");
        if (!File.Exists(scriptPath))
        {
            return $"Could not find Playwright installer at {scriptPath}. Build the app first, then try again.";
        }

        var result = await RunInstallerAsync("pwsh", scriptPath);
        if (result is not null)
        {
            return result;
        }

        result = await RunInstallerAsync("powershell.exe", scriptPath);
        return result ?? "Could not start PowerShell to install Playwright Chromium.";
    }

    private static async Task<string?> RunInstallerAsync(string powerShellExecutable, string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = powerShellExecutable,
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" install chromium",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Win32Exception)
        {
            return null;
        }

        if (process is null)
        {
            return null;
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return process.ExitCode == 0
            ? "Playwright Chromium installed."
            : $"Playwright Chromium install failed using {powerShellExecutable}: {error.Trim()}\n{output.Trim()}";
    }
}
