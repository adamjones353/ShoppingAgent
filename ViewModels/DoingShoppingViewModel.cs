using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Automation;
using ShoppingAgent.Services.ShoppingBridge;

namespace ShoppingAgent.ViewModels;

public sealed class DoingShoppingViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IShoppingListService _shoppingLists;
    private readonly IProductMappingRepository _productMappings;
    private readonly IBrowserAutomationService _automation;
    private readonly IShoppingBridgeState _bridgeState;
    private ShoppingList? _selectedList;
    private ShoppingListItem? _currentItem;
    private ProductMappingCaptureResult? _currentCandidate;
    private string _currentTescoUrl = "";
    private string _status = "";
    private bool _isShopping;
    private CancellationTokenSource? _shoppingCancellation;

    public DoingShoppingViewModel(
        IShoppingListService shoppingLists,
        IProductMappingRepository productMappings,
        IBrowserAutomationService automation,
        IShoppingBridgeState bridgeState)
    {
        _shoppingLists = shoppingLists;
        _productMappings = productMappings;
        _automation = automation;
        _bridgeState = bridgeState;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        StartShoppingCommand = new AsyncRelayCommand(StartShoppingAsync, () => SelectedList is not null);
        NextItemCommand = new AsyncRelayCommand(NextItemAsync, () => SelectedList is not null);
        AcceptCandidateCommand = new AsyncRelayCommand(AcceptCandidateAsync, () => CurrentItem is not null && CurrentCandidate is not null);
        RejectCandidateCommand = new AsyncRelayCommand(RejectCandidateAsync, () => CurrentItem is not null);
        StopShoppingCommand = new AsyncRelayCommand(StopShoppingAsync, () => IsShopping);
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => IsShopping);
        OpenDeliverySlotsCommand = new AsyncRelayCommand(OpenDeliverySlotsAsync, () => IsShopping);
        ResumeShoppingCommand = new AsyncRelayCommand(ResumeShoppingAsync, () => IsShopping);
        ConfirmManualBasketCommand = new AsyncRelayCommand(ConfirmManualBasketAsync, () => CurrentItem is not null);
    }

    public ObservableCollection<ShoppingList> Lists { get; } = [];
    public ObservableCollection<ShoppingListItem> Items { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand StartShoppingCommand { get; }
    public ICommand NextItemCommand { get; }
    public ICommand AcceptCandidateCommand { get; }
    public ICommand RejectCandidateCommand { get; }
    public ICommand StopShoppingCommand { get; }
    public ICommand LoginCommand { get; }
    public ICommand OpenDeliverySlotsCommand { get; }
    public ICommand ResumeShoppingCommand { get; }
    public ICommand ConfirmManualBasketCommand { get; }

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

                _bridgeState.SetActiveList(value);

                RaiseCommandStates();
            }
        }
    }

    public ShoppingListItem? CurrentItem
    {
        get => _currentItem;
        private set
        {
            SetProperty(ref _currentItem, value);
            RaiseCommandStates();
        }
    }

    public ProductMappingCaptureResult? CurrentCandidate
    {
        get => _currentCandidate;
        private set
        {
            SetProperty(ref _currentCandidate, value);
            OnPropertyChanged(nameof(CandidateProductName));
            OnPropertyChanged(nameof(CandidateProductUrl));
            RaiseCommandStates();
        }
    }

    public string CandidateProductName => CurrentCandidate?.ProductName ?? "";
    public string CandidateProductUrl => CurrentCandidate?.ProductUrl ?? "";
    public string CurrentTescoUrl { get => _currentTescoUrl; private set => SetProperty(ref _currentTescoUrl, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool IsShopping
    {
        get => _isShopping;
        private set
        {
            SetProperty(ref _isShopping, value);
            RaiseCommandStates();
        }
    }

    public async Task LoadAsync()
    {
        Lists.Clear();
        foreach (var list in await _shoppingLists.GetListsAsync())
        {
            Lists.Add(list);
        }

        SelectedList = Lists.FirstOrDefault();
    }

    private async Task StartShoppingAsync()
    {
        _shoppingCancellation?.Cancel();
        _shoppingCancellation?.Dispose();
        _shoppingCancellation = new CancellationTokenSource();
        IsShopping = true;

        Status = "Opening Tesco login in your normal browser...";
        var result = await _automation.OpenTescoLoginInDefaultBrowserAsync();
        Status = result.Message;
        if (!result.Succeeded)
        {
            IsShopping = false;
        }
    }

    private async Task NextItemAsync()
    {
        if (SelectedList is null)
        {
            return;
        }

        CurrentCandidate = null;
        CurrentItem = Items.FirstOrDefault(x => !x.CheckedOff && !x.AlreadyOwned && x.Id != CurrentItem?.Id)
            ?? Items.FirstOrDefault(x => !x.CheckedOff && !x.AlreadyOwned);

        if (CurrentItem is null)
        {
            Status = "Shopping list is complete.";
            return;
        }

        ProductMapping? mapping = null;
        if (CurrentItem.IngredientId is not null)
        {
            Status = $"Checking saved Tesco mapping for {CurrentItem.Name}...";
            mapping = await _productMappings.GetPreferredMappingAsync(CurrentItem.IngredientId.Value, "Tesco");
        }

        var searchTerm = mapping?.SearchTerm;
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = CurrentItem.Name;
        }

        CurrentTescoUrl = !string.IsNullOrWhiteSpace(mapping?.ProductUrl)
            ? mapping.ProductUrl
            : $"https://www.tesco.com/groceries/en-GB/search?query={Uri.EscapeDataString(searchTerm)}";

        Status = mapping is not null && !string.IsNullOrWhiteSpace(mapping.ProductUrl)
            ? $"Opening preferred Tesco product for {CurrentItem.Name} in your normal browser..."
            : $"Opening Tesco search for {CurrentItem.Name} in your normal browser...";
        if (_shoppingCancellation?.IsCancellationRequested == true)
        {
            Status = "Shopping automation stopped.";
            return;
        }

        var result = await _automation.OpenShoppingItemInDefaultBrowserAsync(searchTerm, mapping?.ProductUrl ?? "");
        Status = result.Message;
    }

    private async Task AcceptCandidateAsync()
    {
        if (CurrentItem is null || CurrentCandidate is null)
        {
            return;
        }

        Status = $"Adding {CurrentCandidate.ProductName} to basket...";
        var addResult = await _automation.AddCurrentProductToBasketAsync(_shoppingCancellation?.Token ?? CancellationToken.None);
        Status = addResult.Message;
        if (!addResult.Succeeded)
        {
            return;
        }

        CurrentItem.CheckedOff = true;
        await _shoppingLists.PatchItemAsync(CurrentItem.Id, true, CurrentItem.AlreadyOwned);

        if (CurrentItem.IngredientId is not null)
        {
            var save = MessageBox.Show(
                $"Save this as the preferred Tesco product for {CurrentItem.Name}?\n\n{CurrentCandidate.ProductName}",
                "Save preferred product",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (save == MessageBoxResult.Yes)
            {
                Status = $"Saving preferred Tesco product for {CurrentItem.Name}...";
                await _productMappings.SavePreferredMappingAsync(new ProductMappingRequest(
                    CurrentItem.IngredientId.Value,
                    "Tesco",
                    CurrentCandidate.ProductName,
                    CurrentItem.Name,
                    CurrentCandidate.ProductUrl,
                    "",
                    "Saved while doing shopping"));
            }
        }

        await NextItemAsync();
    }

    private async Task StopShoppingAsync()
    {
        _shoppingCancellation?.Cancel();
        var result = await _automation.StopShoppingSessionAsync();
        IsShopping = false;
        CurrentCandidate = null;
        Status = result.Message;
    }

    private async Task LoginAsync()
    {
        Status = "Opening Tesco login in your normal browser...";
        var result = await _automation.OpenTescoLoginInDefaultBrowserAsync();
        Status = result.Message;
    }

    private async Task OpenDeliverySlotsAsync()
    {
        Status = "Opening Tesco delivery slot page...";
        var result = await _automation.OpenDeliverySlotInDefaultBrowserAsync();
        Status = result.Message;
    }

    private async Task ResumeShoppingAsync()
    {
        Status = "Returning to Tesco groceries after delivery slot selection...";
        Status = "Starting item search. The next item will open in your normal browser.";
        await NextItemAsync();
    }

    private async Task RejectCandidateAsync()
    {
        if (CurrentItem is null)
        {
            return;
        }

        Status = $"Skipped {CurrentItem.Name}. Choose another product manually in the browser or press Next Item.";
        CurrentCandidate = null;
        await Task.CompletedTask;
    }

    private async Task ConfirmManualBasketAsync()
    {
        if (CurrentItem is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Have you added '{CurrentItem.Name}' to the Tesco basket in your browser?",
            "Confirm basket item",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            Status = "Item was not marked as done.";
            return;
        }

        CurrentItem.CheckedOff = true;
        await _shoppingLists.PatchItemAsync(CurrentItem.Id, true, CurrentItem.AlreadyOwned);

        if (CurrentItem.IngredientId is not null)
        {
            var save = MessageBox.Show(
                $"Save the current Tesco URL as the preferred product for {CurrentItem.Name}?\n\n{CurrentTescoUrl}",
                "Save preferred product",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (save == MessageBoxResult.Yes)
            {
                await _productMappings.SavePreferredMappingAsync(new ProductMappingRequest(
                    CurrentItem.IngredientId.Value,
                    "Tesco",
                    CurrentItem.Name,
                    CurrentItem.Name,
                    CurrentTescoUrl,
                    "",
                    "Saved from manual Tesco shopping"));
            }
        }

        Status = $"{CurrentItem.Name} marked as done.";
        await NextItemAsync();
    }

    private void RaiseCommandStates()
    {
        ((AsyncRelayCommand)StartShoppingCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)NextItemCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)AcceptCandidateCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RejectCandidateCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)StopShoppingCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)OpenDeliverySlotsCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ResumeShoppingCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ConfirmManualBasketCommand).RaiseCanExecuteChanged();
    }
}
