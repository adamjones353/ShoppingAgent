using ShoppingAgent.Domain;

namespace ShoppingAgent.Services;

public interface IShoppingListService
{
    Task<ShoppingList> GenerateFromPlanAsync(int weeklyMealPlanId);
    Task<List<ShoppingList>> GetListsAsync();
    Task PatchItemAsync(int itemId, bool? checkedOff, bool? alreadyOwned);
    Task RemoveItemAsync(int itemId);
}
