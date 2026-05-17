using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShoppingAgent.Data;
using ShoppingAgent.Repositories;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Ai;
using ShoppingAgent.Services.Automation;
using ShoppingAgent.Services.Settings;
using ShoppingAgent.Services.ShoppingBridge;
using ShoppingAgent.ViewModels;

namespace ShoppingAgent;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                var dataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShoppingAgent");
                Directory.CreateDirectory(dataDirectory);
                var connectionString = $"Data Source={Path.Combine(dataDirectory, "shopping-agent.db")}";

                services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
                services.AddHttpClient<OpenAiClient>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IAiUsageService, AiUsageService>();
                services.AddSingleton<IOpenAiMealSuggestionService, OpenAiMealSuggestionService>();
                services.AddSingleton<IBrowserAiFallbackService, BrowserAiFallbackService>();
                services.AddSingleton<IPlaywrightSetupService, PlaywrightSetupService>();
                services.AddSingleton<IChromeExtensionSetupService, ChromeExtensionSetupService>();
                services.AddSingleton<ILearnedControlService, LearnedControlService>();
                services.AddSingleton<IBrowserAutomationService, TescoAutomationService>();
                services.AddSingleton<IMealRepository, MealRepository>();
                services.AddSingleton<IMealPlanningService, MealPlanningService>();
                services.AddSingleton<IShoppingListService, ShoppingListService>();
                services.AddSingleton<IProductMappingRepository, ProductMappingRepository>();
                services.AddSingleton<IShoppingBridgeState, ShoppingBridgeState>();
                services.AddHostedService<ShoppingBridgeServer>();
                services.AddSingleton<IAppInitializer, AppInitializer>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<MealsViewModel>();
                services.AddTransient<MealSuggestionsViewModel>();
                services.AddTransient<MealHistoryViewModel>();
                services.AddTransient<WeeklyPlannerViewModel>();
                services.AddTransient<ShoppingListViewModel>();
                services.AddTransient<DoingShoppingViewModel>();
                services.AddTransient<ProductMappingsViewModel>();
                services.AddTransient<AutomationSettingsViewModel>();
                services.AddTransient<AiUsageLogsViewModel>();
                services.AddTransient<LearnedControlsViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await _host.StartAsync();
        await _host.Services.GetRequiredService<IAppInitializer>().InitializeAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync(TimeSpan.FromSeconds(5));
        _host.Dispose();
        base.OnExit(e);
    }
}
