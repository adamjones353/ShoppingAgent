using System.ComponentModel.DataAnnotations;

namespace ShoppingAgent.Domain;

public sealed class Meal
{
    public int Id { get; set; }
    [MaxLength(160)] public required string Name { get; set; }
    [MaxLength(1000)] public string Description { get; set; } = "";
    public List<string> CookingSteps { get; set; } = [];
    public PrepEffort PrepEffort { get; set; }
    public int CookingTimeMinutes { get; set; }
    public int Portions { get; set; } = 4;
    public List<string> Tags { get; set; } = [];
    public MealSource Source { get; set; }
    public bool Approved { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<MealIngredient> Ingredients { get; set; } = [];
}

public sealed class Ingredient
{
    public int Id { get; set; }
    [MaxLength(120)] public required string Name { get; set; }
    [MaxLength(80)] public string Category { get; set; } = "Other";
    [MaxLength(40)] public string DefaultUnit { get; set; } = "each";
    [MaxLength(500)] public string Notes { get; set; } = "";
}

public sealed class MealIngredient
{
    public int MealId { get; set; }
    public Meal? Meal { get; set; }
    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public decimal Quantity { get; set; }
    [MaxLength(40)] public string Unit { get; set; } = "";
    public bool Optional { get; set; }
}

public sealed class MealHistoryEntry
{
    public int Id { get; set; }
    public int MealId { get; set; }
    public Meal? Meal { get; set; }
    public DateOnly Date { get; set; }
    public int? Rating { get; set; }
    public bool WouldHaveAgain { get; set; } = true;
    [MaxLength(1000)] public string Notes { get; set; } = "";
}

public sealed class WeeklyMealPlan
{
    public int Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    [MaxLength(160)] public string Name { get; set; } = "";
    public List<PlannedMeal> PlannedMeals { get; set; } = [];
}

public sealed class PlannedMeal
{
    public int Id { get; set; }
    public int WeeklyMealPlanId { get; set; }
    public WeeklyMealPlan? WeeklyMealPlan { get; set; }
    public DateOnly Date { get; set; }
    public PlannedMealSlot Slot { get; set; } = PlannedMealSlot.Dinner;
    public int MealId { get; set; }
    public Meal? Meal { get; set; }
    [MaxLength(500)] public string Notes { get; set; } = "";
}

public sealed class ShoppingList
{
    public int Id { get; set; }
    public int? WeeklyMealPlanId { get; set; }
    public WeeklyMealPlan? WeeklyMealPlan { get; set; }
    [MaxLength(160)] public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ShoppingListItem> Items { get; set; } = [];
}

public sealed class ShoppingListItem
{
    public int Id { get; set; }
    public int ShoppingListId { get; set; }
    public ShoppingList? ShoppingList { get; set; }
    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    [MaxLength(160)] public required string Name { get; set; }
    [MaxLength(80)] public string Category { get; set; } = "Other";
    public decimal Quantity { get; set; }
    [MaxLength(40)] public string Unit { get; set; } = "";
    public bool CheckedOff { get; set; }
    public bool AlreadyOwned { get; set; }
    public bool ManualItem { get; set; }
    [MaxLength(500)] public string Notes { get; set; } = "";
}

public sealed class ProductMapping
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    [MaxLength(80)] public required string SupermarketName { get; set; }
    [MaxLength(200)] public required string ProductName { get; set; }
    [MaxLength(200)] public string SearchTerm { get; set; } = "";
    [MaxLength(1000)] public string ProductUrl { get; set; } = "";
    [MaxLength(80)] public string PreferredQuantity { get; set; } = "";
    [MaxLength(500)] public string Notes { get; set; } = "";
    public DateTimeOffset? LastUsedAt { get; set; }
}

public sealed class AiUsageLog
{
    public int Id { get; set; }
    [MaxLength(120)] public required string Purpose { get; set; }
    [MaxLength(80)] public required string Model { get; set; }
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public bool Succeeded { get; set; }
    [MaxLength(1000)] public string Error { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AppSetting
{
    [MaxLength(120)] public required string Key { get; set; }
    [MaxLength(2000)] public string Value { get; set; } = "";
}

public sealed class LearnedBrowserControl
{
    public int Id { get; set; }
    [MaxLength(80)] public required string SiteName { get; set; }
    [MaxLength(80)] public required string PageType { get; set; }
    [MaxLength(120)] public required string Purpose { get; set; }
    public LocatorType LocatorType { get; set; }
    [MaxLength(1000)] public required string LocatorValue { get; set; }
    [MaxLength(80)] public string AccessibleRole { get; set; } = "";
    [MaxLength(200)] public string AccessibleName { get; set; } = "";
    [MaxLength(500)] public string UrlPattern { get; set; } = "";
    public double ConfidenceScore { get; set; } = 0.5;
    public DateTimeOffset? LastSuccessfulUse { get; set; }
    public int FailureCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
