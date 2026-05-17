using System.Collections.ObjectModel;
using System.Windows.Input;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;

namespace ShoppingAgent.ViewModels;

public sealed class MealHistoryViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IMealRepository _meals;
    private Meal? _selectedMeal;
    private int? _rating = 4;
    private bool _wouldHaveAgain = true;
    private string _notes = "";

    public MealHistoryViewModel(IMealRepository meals)
    {
        _meals = meals;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddHistoryCommand = new AsyncRelayCommand(AddHistoryAsync, () => SelectedMeal is not null);
    }

    public ObservableCollection<Meal> Meals { get; } = [];
    public ObservableCollection<MealHistoryEntry> History { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand AddHistoryCommand { get; }

    public Meal? SelectedMeal { get => _selectedMeal; set { SetProperty(ref _selectedMeal, value); ((AsyncRelayCommand)AddHistoryCommand).RaiseCanExecuteChanged(); } }
    public int? Rating { get => _rating; set => SetProperty(ref _rating, value); }
    public bool WouldHaveAgain { get => _wouldHaveAgain; set => SetProperty(ref _wouldHaveAgain, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public async Task LoadAsync()
    {
        Meals.Clear();
        foreach (var meal in await _meals.GetMealsAsync(true))
        {
            Meals.Add(meal);
        }

        History.Clear();
        foreach (var entry in await _meals.GetHistoryAsync())
        {
            History.Add(entry);
        }
    }

    private async Task AddHistoryAsync()
    {
        if (SelectedMeal is null)
        {
            return;
        }

        await _meals.AddHistoryAsync(new MealHistoryRequest(SelectedMeal.Id, DateOnly.FromDateTime(DateTime.Today), Rating, WouldHaveAgain, Notes));
        Notes = "";
        await LoadAsync();
    }
}
