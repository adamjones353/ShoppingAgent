using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;
using ShoppingAgent.Services.Ai;

namespace ShoppingAgent.ViewModels;

public sealed class MealSuggestionsViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IMealRepository _meals;
    private readonly IOpenAiMealSuggestionService _ai;
    private string _prompt = "Suggest 5 low-effort meals using mostly chicken or bacon. Avoid meals eaten recently.";
    private string _status = "";
    private Meal? _selectedSuggestion;

    public MealSuggestionsViewModel(IMealRepository meals, IOpenAiMealSuggestionService ai)
    {
        _meals = meals;
        _ai = ai;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AskAiCommand = new AsyncRelayCommand(AskAiAsync);
        ApproveCommand = new AsyncRelayCommand(ApproveAsync, () => SelectedSuggestion is not null);
        RemoveSuggestionCommand = new AsyncRelayCommand(RemoveSuggestionAsync, () => SelectedSuggestion is not null);
    }

    public ObservableCollection<Meal> Suggestions { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand AskAiCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand RemoveSuggestionCommand { get; }

    public string Prompt { get => _prompt; set => SetProperty(ref _prompt, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public Meal? SelectedSuggestion
    {
        get => _selectedSuggestion;
        set
        {
            SetProperty(ref _selectedSuggestion, value);
            ((AsyncRelayCommand)ApproveCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RemoveSuggestionCommand).RaiseCanExecuteChanged();
        }
    }

    public async Task LoadAsync()
    {
        Suggestions.Clear();
        foreach (var meal in await _meals.GetSuggestionsAsync())
        {
            Suggestions.Add(meal);
        }
    }

    private async Task AskAiAsync()
    {
        Status = "Requesting suggestions...";
        var history = await _meals.GetHistoryAsync();
        var result = await _ai.SuggestMealsAsync(Prompt, history.Select(x => x.Meal?.Name ?? "").Where(x => x.Length > 0).ToList());
        foreach (var meal in result.Meals)
        {
            await _meals.UpsertMealAsync(null, new UpsertMealRequest(
                meal.Name,
                meal.Description,
                meal.Ingredients,
                meal.CookingSteps,
                meal.PrepEffort,
                meal.CookingTimeMinutes,
                meal.Portions,
                meal.Tags,
                false), MealSource.AiSuggested);
        }

        Status = $"Saved {result.Meals.Count} AI suggestions for review.";
        await LoadAsync();
    }

    private async Task ApproveAsync()
    {
        if (SelectedSuggestion is null)
        {
            return;
        }

        await _meals.ApproveMealAsync(SelectedSuggestion.Id);
        SelectedSuggestion = null;
        await LoadAsync();
    }

    private async Task RemoveSuggestionAsync()
    {
        if (SelectedSuggestion is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Remove suggested meal '{SelectedSuggestion.Name}'?",
            "Remove suggestion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await _meals.DeleteMealAsync(SelectedSuggestion.Id);
        Status = "Suggestion removed.";
        SelectedSuggestion = null;
        await LoadAsync();
    }
}
