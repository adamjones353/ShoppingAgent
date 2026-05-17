using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Contracts;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services;

public sealed class MealPlanningService(IDbContextFactory<AppDbContext> dbFactory) : IMealPlanningService
{
    public async Task<WeeklyMealPlan> GetOrCreateWeekAsync(DateOnly weekStartDate)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var plan = await db.WeeklyMealPlans
            .Include(x => x.PlannedMeals)
            .ThenInclude(x => x.Meal)
            .FirstOrDefaultAsync(x => x.WeekStartDate == weekStartDate);

        if (plan is not null)
        {
            return plan;
        }

        plan = new WeeklyMealPlan
        {
            WeekStartDate = weekStartDate,
            Name = $"Week of {weekStartDate:yyyy-MM-dd}"
        };
        db.WeeklyMealPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<List<Meal>> SuggestMealsAsync(AutoSuggestRequest request)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var recentCutoff = request.WeekStartDate.AddDays(-21);
        var recentlyEatenIds = await db.MealHistory
            .Where(x => x.Date >= recentCutoff)
            .Select(x => x.MealId)
            .Distinct()
            .ToListAsync();

        var dislikedIds = await db.MealHistory
            .Where(x => !x.WouldHaveAgain)
            .Select(x => x.MealId)
            .Distinct()
            .ToListAsync();

        var excluded = recentlyEatenIds.Concat(dislikedIds).Concat(request.ExcludedMealIds).ToHashSet();
        var meals = await db.Meals
            .Include(x => x.Ingredients)
            .ThenInclude(x => x.Ingredient)
            .Where(x => x.Approved && !excluded.Contains(x.Id))
            .ToListAsync();

        if (request.Tags.Count > 0)
        {
            meals = meals.Where(x => request.Tags.All(tag => x.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))).ToList();
        }

        return meals
            .OrderBy(x => x.PrepEffort == PrepEffort.Low ? 0 : x.PrepEffort == PrepEffort.Medium ? 1 : 2)
            .ThenByDescending(x => x.Tags.Contains("batch-cook") || x.Tags.Contains("leftovers"))
            .ThenBy(x => x.CookingTimeMinutes)
            .Take(7)
            .ToList();
    }

    public async Task AddOrReplacePlannedMealAsync(int weeklyMealPlanId, PlanMealRequest request)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.PlannedMeals.FirstOrDefaultAsync(x =>
            x.WeeklyMealPlanId == weeklyMealPlanId &&
            x.Date == request.Date &&
            x.Slot == request.Slot);

        if (existing is null)
        {
            db.PlannedMeals.Add(new PlannedMeal
            {
                WeeklyMealPlanId = weeklyMealPlanId,
                Date = request.Date,
                Slot = request.Slot,
                MealId = request.MealId,
                Notes = request.Notes
            });
        }
        else
        {
            existing.MealId = request.MealId;
            existing.Notes = request.Notes;
        }

        await db.SaveChangesAsync();
    }

    public async Task RemovePlannedMealAsync(int plannedMealId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var plannedMeal = await db.PlannedMeals.FirstOrDefaultAsync(x => x.Id == plannedMealId);
        if (plannedMeal is null)
        {
            return;
        }

        db.PlannedMeals.Remove(plannedMeal);
        await db.SaveChangesAsync();
    }
}
