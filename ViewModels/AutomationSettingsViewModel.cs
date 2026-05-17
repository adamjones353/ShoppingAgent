using System.Windows.Input;
using ShoppingAgent.Services.Automation;
using ShoppingAgent.Services.Settings;

namespace ShoppingAgent.ViewModels;

public sealed class AutomationSettingsViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IPlaywrightSetupService _playwrightSetup;
    private readonly IChromeExtensionSetupService _extensionSetup;
    private AppSettingsModel _settings = new();
    private string _status = "";

    public AutomationSettingsViewModel(ISettingsService settingsService, IPlaywrightSetupService playwrightSetup, IChromeExtensionSetupService extensionSetup)
    {
        _settingsService = settingsService;
        _playwrightSetup = playwrightSetup;
        _extensionSetup = extensionSetup;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        InstallChromiumCommand = new AsyncRelayCommand(InstallChromiumAsync);
        InstallExtensionCommand = new AsyncRelayCommand(InstallExtensionAsync);
    }

    public AppSettingsModel Settings { get => _settings; set => SetProperty(ref _settings, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public ICommand SaveCommand { get; }
    public ICommand InstallChromiumCommand { get; }
    public ICommand InstallExtensionCommand { get; }

    public async Task LoadAsync() => Settings = await _settingsService.GetSettingsAsync();

    private async Task SaveAsync()
    {
        await _settingsService.SaveSettingsAsync(Settings);
        Status = "Settings saved locally.";
    }

    private async Task InstallChromiumAsync()
    {
        Status = "Installing Playwright Chromium...";
        Status = await _playwrightSetup.InstallChromiumAsync();
    }

    private async Task InstallExtensionAsync()
    {
        Status = await _extensionSetup.OpenInstallInstructionsAsync();
    }
}
