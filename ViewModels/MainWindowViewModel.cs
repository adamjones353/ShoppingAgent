using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace ShoppingAgent.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;

    public MainWindowViewModel(IServiceProvider services)
    {
        NavigationItems =
        [
            new("Dashboard", () => services.GetRequiredService<DashboardViewModel>()),
            new("Meals", () => services.GetRequiredService<MealsViewModel>()),
            new("Meal Suggestions", () => services.GetRequiredService<MealSuggestionsViewModel>()),
            new("Meal History", () => services.GetRequiredService<MealHistoryViewModel>()),
            new("Weekly Planner", () => services.GetRequiredService<WeeklyPlannerViewModel>()),
            new("Shopping List", () => services.GetRequiredService<ShoppingListViewModel>()),
            new("Doing Shopping", () => services.GetRequiredService<DoingShoppingViewModel>()),
            new("Product Mappings", () => services.GetRequiredService<ProductMappingsViewModel>()),
            new("Automation Settings", () => services.GetRequiredService<AutomationSettingsViewModel>()),
            new("AI Usage Logs", () => services.GetRequiredService<AiUsageLogsViewModel>()),
            new("Learned Controls", () => services.GetRequiredService<LearnedControlsViewModel>())
        ];

        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value) && value is not null)
            {
                CurrentViewModel = value.Factory();
                if (CurrentViewModel is ILoadableViewModel loadable)
                {
                    _ = loadable.LoadAsync();
                }
            }
        }
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }
}
