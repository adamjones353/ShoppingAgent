using ShoppingAgent.Domain;
using ShoppingAgent.Repositories;

namespace ShoppingAgent.Services.ShoppingBridge;

public sealed class ShoppingBridgeState(IProductMappingRepository mappings) : IShoppingBridgeState
{
    private readonly object _lock = new();
    private List<ShoppingListItem> _items = [];
    private int _currentIndex;

    public void SetActiveList(ShoppingList? list)
    {
        lock (_lock)
        {
            _items = list?.Items
                .Where(x => !x.CheckedOff && !x.AlreadyOwned)
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToList() ?? [];
            _currentIndex = 0;
        }
    }

    public ShoppingBridgeItem? GetCurrentItem()
    {
        ShoppingListItem? item;
        lock (_lock)
        {
            item = _items.ElementAtOrDefault(_currentIndex);
        }

        return item is null ? null : ToBridgeItemAsync(item).GetAwaiter().GetResult();
    }

    public ShoppingBridgeItem? MoveNext()
    {
        lock (_lock)
        {
            if (_currentIndex < _items.Count)
            {
                _currentIndex++;
            }
        }

        return GetCurrentItem();
    }

    private async Task<ShoppingBridgeItem> ToBridgeItemAsync(ShoppingListItem item)
    {
        var searchTerm = item.Name;
        var productUrl = "";
        if (item.IngredientId is not null)
        {
            var mapping = await mappings.GetPreferredMappingAsync(item.IngredientId.Value, "Tesco");
            searchTerm = string.IsNullOrWhiteSpace(mapping?.SearchTerm) ? item.Name : mapping.SearchTerm;
            productUrl = mapping?.ProductUrl ?? "";
        }

        return new ShoppingBridgeItem(item.Id, item.Name, item.Quantity, item.Unit, item.IngredientId, searchTerm, productUrl);
    }
}
