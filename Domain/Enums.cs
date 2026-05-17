namespace ShoppingAgent.Domain;

public enum PrepEffort
{
    Low,
    Medium,
    High
}

public enum MealSource
{
    Preloaded,
    UserCreated,
    AiSuggested
}

public enum PlannedMealSlot
{
    Breakfast,
    Lunch,
    Dinner
}

public enum LocatorType
{
    Role,
    Label,
    Placeholder,
    Text,
    Css,
    XPath
}
