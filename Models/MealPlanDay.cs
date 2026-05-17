using ShoppingAgent.Domain;

namespace ShoppingAgent.Models;

public sealed class MealPlanDay
{
    public DateOnly Date { get; set; }
    public PlannedMeal? Breakfast { get; set; }
    public PlannedMeal? Lunch { get; set; }
    public PlannedMeal? Dinner { get; set; }
}
