using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;
using ShoppingAgent.Services.Automation;

namespace ShoppingAgent.ViewModels;

public sealed class ProductMappingsViewModel : ViewModelBase, ILoadableViewModel
{
    private readonly IProductMappingRepository _mappings;
    private readonly IMealRepository _meals;
    private readonly IBrowserAutomationService _automation;
    private Ingredient? _selectedIngredient;
    private ProductMapping? _selectedMapping;
    private string _productName = "";
    private string _searchTerm = "";
    private string _productUrl = "";
    private string _status = "";
    private bool _mappingSessionActive;

    public ProductMappingsViewModel(IProductMappingRepository mappings, IMealRepository meals, IBrowserAutomationService automation)
    {
        _mappings = mappings;
        _meals = meals;
        _automation = automation;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddMappingCommand = new AsyncRelayCommand(AddMappingAsync, () => SelectedIngredient is not null && !string.IsNullOrWhiteSpace(ProductName));
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        StartAssistedMappingCommand = new AsyncRelayCommand(StartAssistedMappingAsync, () => SelectedIngredient is not null);
        ConfirmCurrentProductCommand = new AsyncRelayCommand(ConfirmCurrentProductAsync, () => SelectedIngredient is not null && MappingSessionActive);
    }

    public ObservableCollection<Ingredient> Ingredients { get; } = [];
    public ObservableCollection<ProductMapping> Mappings { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand AddMappingCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand StartAssistedMappingCommand { get; }
    public ICommand ConfirmCurrentProductCommand { get; }

    public Ingredient? SelectedIngredient
    {
        get => _selectedIngredient;
        set
        {
            SetProperty(ref _selectedIngredient, value);
            ((AsyncRelayCommand)AddMappingCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)StartAssistedMappingCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ConfirmCurrentProductCommand).RaiseCanExecuteChanged();
            if (value is not null && string.IsNullOrWhiteSpace(SearchTerm))
            {
                SearchTerm = value.Name;
            }
        }
    }

    public ProductMapping? SelectedMapping
    {
        get => _selectedMapping;
        set
        {
            if (SetProperty(ref _selectedMapping, value) && value is not null)
            {
                SelectedIngredient = Ingredients.FirstOrDefault(x => x.Id == value.IngredientId);
                ProductName = value.ProductName;
                SearchTerm = value.SearchTerm;
                ProductUrl = value.ProductUrl;
            }
        }
    }

    public string ProductName { get => _productName; set { SetProperty(ref _productName, value); ((AsyncRelayCommand)AddMappingCommand).RaiseCanExecuteChanged(); } }
    public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }
    public string ProductUrl { get => _productUrl; set => SetProperty(ref _productUrl, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool MappingSessionActive
    {
        get => _mappingSessionActive;
        private set
        {
            SetProperty(ref _mappingSessionActive, value);
            ((AsyncRelayCommand)ConfirmCurrentProductCommand).RaiseCanExecuteChanged();
        }
    }

    public async Task LoadAsync()
    {
        Ingredients.Clear();
        foreach (var ingredient in await _meals.GetIngredientsAsync())
        {
            Ingredients.Add(ingredient);
        }

        Mappings.Clear();
        foreach (var mapping in await _mappings.GetMappingsAsync())
        {
            Mappings.Add(mapping);
        }
    }

    private async Task AddMappingAsync()
    {
        if (SelectedIngredient is null)
        {
            return;
        }

        await _mappings.AddMappingAsync(new ProductMappingRequest(SelectedIngredient.Id, "Tesco", ProductName, string.IsNullOrWhiteSpace(SearchTerm) ? ProductName : SearchTerm, ProductUrl, "", ""));
        ProductName = "";
        SearchTerm = "";
        ProductUrl = "";
        await LoadAsync();
    }

    private async Task SearchAsync()
    {
        var term = SelectedMapping?.SearchTerm ?? SearchTerm;
        var result = await _automation.SearchProductAsync(term);
        Status = result.Message;
    }

    private async Task StartAssistedMappingAsync()
    {
        if (SelectedIngredient is null)
        {
            return;
        }

        var term = string.IsNullOrWhiteSpace(SearchTerm) ? SelectedIngredient.Name : SearchTerm;
        SearchTerm = term;
        var result = await _automation.StartProductMappingAsync(term);
        MappingSessionActive = result.Succeeded;
        Status = result.Message;
    }

    private async Task ConfirmCurrentProductAsync()
    {
        if (SelectedIngredient is null)
        {
            return;
        }

        var capture = await _automation.CaptureCurrentProductAsync();
        if (!capture.Succeeded)
        {
            Status = capture.Message;
            return;
        }

        ProductName = capture.ProductName;
        ProductUrl = capture.ProductUrl;
        var confirmation = MessageBox.Show(
            $"Save this Tesco product as the preferred item for {SelectedIngredient.Name}?\n\n{ProductName}\n{ProductUrl}",
            "Confirm preferred product",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
        {
            Status = "Product mapping was not saved.";
            return;
        }

        await _mappings.SavePreferredMappingAsync(new ProductMappingRequest(
            SelectedIngredient.Id,
            "Tesco",
            ProductName,
            string.IsNullOrWhiteSpace(SearchTerm) ? SelectedIngredient.Name : SearchTerm,
            ProductUrl,
            "",
            "Confirmed from assisted Tesco mapping"));

        Status = $"Saved {ProductName} as preferred Tesco product for {SelectedIngredient.Name}.";
        MappingSessionActive = false;
        await LoadAsync();
    }
}
