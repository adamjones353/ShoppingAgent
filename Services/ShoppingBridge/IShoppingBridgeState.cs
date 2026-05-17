using ShoppingAgent.Domain;

namespace ShoppingAgent.Services.ShoppingBridge;

public interface IShoppingBridgeState
{
    void SetActiveList(ShoppingList? list);
    ShoppingBridgeItem? GetCurrentItem();
    ShoppingBridgeItem? MoveNext();
}
