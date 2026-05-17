using ShoppingAgent.Domain;

namespace ShoppingAgent.Contracts;

public sealed record MealIngredientDto(int IngredientId, string Name, string Category, decimal Quantity, string Unit, bool Optional);
public sealed record MealDto(int Id, string Name, string Description, List<MealIngredientDto> Ingredients, List<string> CookingSteps, PrepEffort PrepEffort, int CookingTimeMinutes, int Portions, List<string> Tags, MealSource Source, bool Approved, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record UpsertMealRequest(string Name, string Description, List<AiMealIngredientDto> Ingredients, List<string> CookingSteps, PrepEffort PrepEffort, int CookingTimeMinutes, int Portions, List<string> Tags, bool Approved);
public sealed record AiMealIngredientDto(string Name, decimal Quantity, string Unit, string Category);
public sealed record MealHistoryRequest(int MealId, DateOnly Date, int? Rating, bool WouldHaveAgain, string Notes);
public sealed record AutoSuggestRequest(DateOnly WeekStartDate, List<string> Tags, List<int> ExcludedMealIds);
public sealed record PlanMealRequest(DateOnly Date, PlannedMealSlot Slot, int MealId, string Notes);
public sealed record AiSuggestMealsRequest(string Prompt);
public sealed record AiSuggestedMeal(string Name, string Description, PrepEffort PrepEffort, int CookingTimeMinutes, int Portions, List<string> Tags, List<AiMealIngredientDto> Ingredients, List<string> CookingSteps);
public sealed record AiSuggestedMealsResponse(List<AiSuggestedMeal> Meals);
public sealed record ProductMappingRequest(int IngredientId, string SupermarketName, string ProductName, string SearchTerm, string ProductUrl, string PreferredQuantity, string Notes);
public sealed record ShoppingItemPatch(bool? CheckedOff, bool? AlreadyOwned, decimal? Quantity, string? Unit, string? Notes);
public sealed record LearnedControlRequest(string SiteName, string PageType, string Purpose, LocatorType LocatorType, string LocatorValue, string AccessibleRole, string AccessibleName, string UrlPattern);
public sealed record BrowserSearchRequest(string SupermarketName, string SearchTerm);
