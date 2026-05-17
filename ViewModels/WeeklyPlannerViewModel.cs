using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;
using ShoppingAgent.Services;

namespace ShoppingAgent.ViewModels;

public sealed class WeeklyPlannerViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IMealRepository _meals;
    private readonly IMealPlanningService _planner;
    private WeeklyMealPlan? _currentPlan;
    private Meal? _selectedMeal;
    private PlannedMeal? _selectedPlannedMeal;
    private string _status = "";

    public WeeklyPlannerViewModel(IMealRepository meals, IMealPlanningService planner)
    {
        _meals = meals;
        _planner = planner;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AutoSuggestCommand = new AsyncRelayCommand(AutoSuggestAsync);
        AddDinnerCommand = new AsyncRelayCommand(AddDinnerAsync, () => SelectedMeal is not null);
        RemovePlannedMealCommand = new AsyncRelayCommand(RemovePlannedMealAsync, () => SelectedPlannedMeal is not null);
    }

    public ObservableCollection<Meal> Meals { get; } = [];
    public ObservableCollection<PlannedMeal> PlannedMeals { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand AutoSuggestCommand { get; }
    public ICommand AddDinnerCommand { get; }
    public ICommand RemovePlannedMealCommand { get; }

    public DateOnly WeekStartDate { get; } = StartOfWeek(DateTime.Today);
    public WeeklyMealPlan? CurrentPlan { get => _currentPlan; private set => SetProperty(ref _currentPlan, value); }
    public Meal? SelectedMeal { get => _selectedMeal; set { SetProperty(ref _selectedMeal, value); ((AsyncRelayCommand)AddDinnerCommand).RaiseCanExecuteChanged(); } }
    public PlannedMeal? SelectedPlannedMeal
    {
        get => _selectedPlannedMeal;
        set
        {
            SetProperty(ref _selectedPlannedMeal, value);
            ((AsyncRelayCommand)RemovePlannedMealCommand).RaiseCanExecuteChanged();
        }
    }

    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public async Task LoadAsync()
    {
        CurrentPlan = await _planner.GetOrCreateWeekAsync(WeekStartDate);
        Meals.Clear();
        foreach (var meal in await _meals.GetMealsAsync(true))
        {
            Meals.Add(meal);
        }

        RefreshPlannedMeals(CurrentPlan);
    }

    private async Task AutoSuggestAsync()
    {
        if (CurrentPlan is null)
        {
            return;
        }

        var suggestions = await _planner.SuggestMealsAsync(new AutoSuggestRequest(WeekStartDate, [], CurrentPlan.PlannedMeals.Select(x => x.MealId).ToList()));
        var day = 0;
        foreach (var meal in suggestions.Take(7))
        {
            await _planner.AddOrReplacePlannedMealAsync(CurrentPlan.Id, new PlanMealRequest(WeekStartDate.AddDays(day++), PlannedMealSlot.Dinner, meal.Id, "Auto-suggested locally"));
        }

        Status = $"Added {day} local suggestions.";
        await LoadAsync();
    }

    private async Task AddDinnerAsync()
    {
        if (CurrentPlan is null || SelectedMeal is null)
        {
            return;
        }

        await _planner.AddOrReplacePlannedMealAsync(CurrentPlan.Id, new PlanMealRequest(DateOnly.FromDateTime(DateTime.Today), PlannedMealSlot.Dinner, SelectedMeal.Id, "Manually selected"));
        await LoadAsync();
    }

    private async Task RemovePlannedMealAsync()
    {
        if (SelectedPlannedMeal is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Remove {SelectedPlannedMeal.Meal?.Name} from {SelectedPlannedMeal.Date} {SelectedPlannedMeal.Slot}?",
            "Remove planned meal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await _planner.RemovePlannedMealAsync(SelectedPlannedMeal.Id);
        Status = "Planned meal removed.";
        SelectedPlannedMeal = null;
        await LoadAsync();
    }

    private void RefreshPlannedMeals(WeeklyMealPlan plan)
    {
        PlannedMeals.Clear();
        foreach (var item in plan.PlannedMeals.OrderBy(x => x.Date).ThenBy(x => x.Slot))
        {
            PlannedMeals.Add(item);
        }
    }

    private static DateOnly StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return DateOnly.FromDateTime(date.AddDays(-diff));
    }
}
