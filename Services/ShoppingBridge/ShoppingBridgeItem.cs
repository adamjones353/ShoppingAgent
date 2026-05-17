namespace ShoppingAgent.Services.ShoppingBridge;

public sealed record ShoppingBridgeItem(
    int Id,
    string Name,
    decimal Quantity,
    string Unit,
    int? IngredientId,
    string SearchTerm,
    string ProductUrl);
