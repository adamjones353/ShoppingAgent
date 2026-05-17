using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services;

public static class MealMapper
{
    public static MealDto ToDto(Meal meal) => new(
        meal.Id,
        meal.Name,
        meal.Description,
        meal.Ingredients
            .OrderBy(x => x.Ingredient!.Category)
            .ThenBy(x => x.Ingredient!.Name)
            .Select(x => new MealIngredientDto(x.IngredientId, x.Ingredient!.Name, x.Ingredient.Category, x.Quantity, x.Unit, x.Optional))
            .ToList(),
        meal.CookingSteps,
        meal.PrepEffort,
        meal.CookingTimeMinutes,
        meal.Portions,
        meal.Tags,
        meal.Source,
        meal.Approved,
        meal.CreatedAt,
        meal.UpdatedAt);
}
