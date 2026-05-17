using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShoppingAgent.Domain;
using ShoppingAgent.Services;

namespace ShoppingAgent.ViewModels;

public sealed class ShoppingListViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IShoppingListService _shoppingLists;
    private readonly IMealPlanningService _planner;
    private ShoppingList? _selectedList;
    private ShoppingListItem? _selectedItem;
    private string _status = "";

    public ShoppingListViewModel(IShoppingListService shoppingLists, IMealPlanningService planner)
    {
        _shoppingLists = shoppingLists;
        _planner = planner;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        GenerateForCurrentWeekCommand = new AsyncRelayCommand(GenerateForCurrentWeekAsync);
        SaveItemStatesCommand = new AsyncRelayCommand(SaveItemStatesAsync);
        RemoveSelectedItemCommand = new AsyncRelayCommand(RemoveSelectedItemAsync, () => SelectedItem is not null);
    }

    public ObservableCollection<ShoppingList> Lists { get; } = [];
    public ObservableCollection<ShoppingListItem> Items { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand GenerateForCurrentWeekCommand { get; }
    public ICommand SaveItemStatesCommand { get; }
    public ICommand RemoveSelectedItemCommand { get; }

    public ShoppingList? SelectedList
    {
        get => _selectedList;
        set
        {
            if (SetProperty(ref _selectedList, value))
            {
                Items.Clear();
                if (value is not null)
                {
                    foreach (var item in value.Items.OrderBy(x => x.Category).ThenBy(x => x.Name))
                    {
                        Items.Add(item);
                    }
                }
            }
        }
    }

    public ShoppingListItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            ((AsyncRelayCommand)RemoveSelectedItemCommand).RaiseCanExecuteChanged();
        }
    }

    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public async Task LoadAsync()
    {
        Lists.Clear();
        foreach (var list in await _shoppingLists.GetListsAsync())
        {
            Lists.Add(list);
        }

        SelectedList = Lists.FirstOrDefault();
    }

    private async Task GenerateForCurrentWeekAsync()
    {
        var weekStart = StartOfWeek(DateTime.Today);
        var plan = await _planner.GetOrCreateWeekAsync(weekStart);
        var list = await _shoppingLists.GenerateFromPlanAsync(plan.Id);
        Status = $"Generated {list.Items.Count} grouped items.";
        await LoadAsync();
    }

    private async Task SaveItemStatesAsync()
    {
        foreach (var item in Items)
        {
            await _shoppingLists.PatchItemAsync(item.Id, item.CheckedOff, item.AlreadyOwned);
        }

        Status = "Shopping list item states saved.";
    }

    private async Task RemoveSelectedItemAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Remove '{SelectedItem.Name}' from this shopping list?",
            "Remove shopping item",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await _shoppingLists.RemoveItemAsync(SelectedItem.Id);
        Items.Remove(SelectedItem);
        SelectedItem = null;
        Status = "Shopping item removed.";
    }

    private static DateOnly StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return DateOnly.FromDateTime(date.AddDays(-diff));
    }
}
