using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Contracts;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Repositories;

public sealed class MealRepository(IDbContextFactory<AppDbContext> dbFactory) : IMealRepository
{
    public async Task<List<Meal>> GetMealsAsync(bool approvedOnly = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Meals.Include(x => x.Ingredients).ThenInclude(x => x.Ingredient).AsQueryable();
        if (approvedOnly)
        {
            query = query.Where(x => x.Approved);
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Meal>> GetSuggestionsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var suggestions = await db.Meals
            .Include(x => x.Ingredients)
            .ThenInclude(x => x.Ingredient)
            .Where(x => x.Source == MealSource.AiSuggested && !x.Approved)
            .ToListAsync();

        return suggestions
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    public async Task<Meal?> GetMealAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Meals.Include(x => x.Ingredients).ThenInclude(x => x.Ingredient).FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Meal> UpsertMealAsync(int? id, UpsertMealRequest request, MealSource source)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var meal = id is null
            ? new Meal { Name = request.Name, Source = source, CreatedAt = DateTimeOffset.UtcNow }
            : await db.Meals.Include(x => x.Ingredients).FirstAsync(x => x.Id == id.Value);

        meal.Name = request.Name.Trim();
        meal.Description = request.Description.Trim();
        meal.CookingSteps = request.CookingSteps.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        meal.PrepEffort = request.PrepEffort;
        meal.CookingTimeMinutes = request.CookingTimeMinutes;
        meal.Portions = request.Portions;
        meal.Tags = request.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        meal.Approved = request.Approved;
        meal.UpdatedAt = DateTimeOffset.UtcNow;

        if (id is null)
        {
            db.Meals.Add(meal);
        }
        else
        {
            db.MealIngredients.RemoveRange(meal.Ingredients);
            meal.Ingredients.Clear();
        }

        foreach (var item in request.Ingredients.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            var ingredientName = item.Name.Trim().ToLowerInvariant();
            var ingredient = await db.Ingredients.FirstOrDefaultAsync(x => x.Name == ingredientName);
            if (ingredient is null)
            {
                ingredient = new Ingredient
                {
                    Name = ingredientName,
                    Category = string.IsNullOrWhiteSpace(item.Category) ? "Other" : item.Category.Trim(),
                    DefaultUnit = string.IsNullOrWhiteSpace(item.Unit) ? "each" : item.Unit.Trim()
                };
                db.Ingredients.Add(ingredient);
            }

            meal.Ingredients.Add(new MealIngredient
            {
                Meal = meal,
                Ingredient = ingredient,
                Quantity = item.Quantity,
                Unit = string.IsNullOrWhiteSpace(item.Unit) ? ingredient.DefaultUnit : item.Unit.Trim()
            });
        }

        await db.SaveChangesAsync();
        return meal;
    }

    public async Task ApproveMealAsync(int mealId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var meal = await db.Meals.FirstAsync(x => x.Id == mealId);
        meal.Approved = true;
        meal.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteMealAsync(int mealId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var meal = await db.Meals
            .Include(x => x.Ingredients)
            .FirstOrDefaultAsync(x => x.Id == mealId);

        if (meal is null)
        {
            return;
        }

        db.Meals.Remove(meal);
        await db.SaveChangesAsync();
    }

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Ingredients.OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync();
    }

    public async Task AddHistoryAsync(MealHistoryRequest request)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.MealHistory.Add(new MealHistoryEntry
        {
            MealId = request.MealId,
            Date = request.Date,
            Rating = request.Rating,
            WouldHaveAgain = request.WouldHaveAgain,
            Notes = request.Notes
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<MealHistoryEntry>> GetHistoryAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.MealHistory.Include(x => x.Meal).OrderByDescending(x => x.Date).Take(100).ToListAsync();
    }
}
