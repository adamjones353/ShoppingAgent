using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;

namespace ShoppingAgent.ViewModels;

public sealed class MealsViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IMealRepository _meals;
    private Meal? _selectedMeal;
    private string _newMealName = "";

    public MealsViewModel(IMealRepository meals)
    {
        _meals = meals;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddQuickMealCommand = new AsyncRelayCommand(AddQuickMealAsync, () => !string.IsNullOrWhiteSpace(NewMealName));
        DeleteSelectedMealCommand = new AsyncRelayCommand(DeleteSelectedMealAsync, () => SelectedMeal is not null);
    }

    public ObservableCollection<Meal> Meals { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand AddQuickMealCommand { get; }
    public ICommand DeleteSelectedMealCommand { get; }

    public Meal? SelectedMeal
    {
        get => _selectedMeal;
        set
        {
            SetProperty(ref _selectedMeal, value);
            ((AsyncRelayCommand)DeleteSelectedMealCommand).RaiseCanExecuteChanged();
        }
    }
    public string NewMealName { get => _newMealName; set { SetProperty(ref _newMealName, value); ((AsyncRelayCommand)AddQuickMealCommand).RaiseCanExecuteChanged(); } }

    public async Task LoadAsync()
    {
        Meals.Clear();
        foreach (var meal in await _meals.GetMealsAsync())
        {
            Meals.Add(meal);
        }
    }

    private async Task AddQuickMealAsync()
    {
        await _meals.UpsertMealAsync(null, new UpsertMealRequest(
            NewMealName,
            "User-created meal.",
            [],
            [],
            PrepEffort.Medium,
            30,
            4,
            ["user"],
            true), MealSource.UserCreated);
        NewMealName = "";
        await LoadAsync();
    }

    private async Task DeleteSelectedMealAsync()
    {
        if (SelectedMeal is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Remove '{SelectedMeal.Name}' from meals?\n\nThis also removes linked planner/history entries for that meal.",
            "Remove meal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await _meals.DeleteMealAsync(SelectedMeal.Id);
        SelectedMeal = null;
        await LoadAsync();
    }
}
