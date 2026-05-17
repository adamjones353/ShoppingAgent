using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services;

public interface IMealPlanningService
{
    Task<WeeklyMealPlan> GetOrCreateWeekAsync(DateOnly weekStartDate);
    Task<List<Meal>> SuggestMealsAsync(AutoSuggestRequest request);
    Task AddOrReplacePlannedMealAsync(int weeklyMealPlanId, PlanMealRequest request);
    Task RemovePlannedMealAsync(int plannedMealId);
}
