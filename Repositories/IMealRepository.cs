using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Repositories;

public interface IMealRepository
{
    Task<List<Meal>> GetMealsAsync(bool approvedOnly = false);
    Task<List<Meal>> GetSuggestionsAsync();
    Task<Meal?> GetMealAsync(int id);
    Task<Meal> UpsertMealAsync(int? id, UpsertMealRequest request, MealSource source);
    Task ApproveMealAsync(int mealId);
    Task DeleteMealAsync(int mealId);
    Task<List<Ingredient>> GetIngredientsAsync();
    Task AddHistoryAsync(MealHistoryRequest request);
    Task<List<MealHistoryEntry>> GetHistoryAsync();
}
