using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Data;

namespace ShoppingAgent.ViewModels;

public sealed class DashboardViewModel(IDbContextFactory<AppDbContext> dbFactory) : ViewModelBase, ILoadableViewModel
{
    private int _mealCount;
    private int _suggestionCount;
    private int _historyCount;
    private int _shoppingListCount;

    public int MealCount { get => _mealCount; private set => SetProperty(ref _mealCount, value); }
    public int SuggestionCount { get => _suggestionCount; private set => SetProperty(ref _suggestionCount, value); }
    public int HistoryCount { get => _historyCount; private set => SetProperty(ref _historyCount, value); }
    public int ShoppingListCount { get => _shoppingListCount; private set => SetProperty(ref _shoppingListCount, value); }

    public async Task LoadAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        MealCount = await db.Meals.CountAsync(x => x.Approved);
        SuggestionCount = await db.Meals.CountAsync(x => !x.Approved);
        HistoryCount = await db.MealHistory.CountAsync();
        ShoppingListCount = await db.ShoppingLists.CountAsync();
    }
}
