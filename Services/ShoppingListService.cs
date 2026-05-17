using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services;

public sealed class ShoppingListService(IDbContextFactory<AppDbContext> dbFactory) : IShoppingListService
{
    public async Task<ShoppingList> GenerateFromPlanAsync(int weeklyMealPlanId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var plan = await db.WeeklyMealPlans
            .Include(x => x.PlannedMeals)
            .ThenInclude(x => x.Meal)
            .ThenInclude(x => x!.Ingredients)
            .ThenInclude(x => x.Ingredient)
            .FirstAsync(x => x.Id == weeklyMealPlanId);

        var list = new ShoppingList
        {
            WeeklyMealPlanId = plan.Id,
            Name = $"Shopping list for {plan.WeekStartDate:yyyy-MM-dd}"
        };

        var grouped = plan.PlannedMeals
            .SelectMany(x => x.Meal!.Ingredients)
            .Where(x => !x.Optional)
            .GroupBy(x => new { x.IngredientId, x.Unit })
            .OrderBy(x => x.First().Ingredient!.Category)
            .ThenBy(x => x.First().Ingredient!.Name);

        foreach (var group in grouped)
        {
            var ingredient = group.First().Ingredient!;
            list.Items.Add(new ShoppingListItem
            {
                IngredientId = ingredient.Id,
                Name = ingredient.Name,
                Category = ingredient.Category,
                Unit = group.Key.Unit,
                Quantity = group.Sum(x => x.Quantity)
            });
        }

        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync();
        return await db.ShoppingLists
            .Include(x => x.Items.OrderBy(i => i.Category).ThenBy(i => i.Name))
            .FirstAsync(x => x.Id == list.Id);
    }

    public async Task<List<ShoppingList>> GetListsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var lists = await db.ShoppingLists.Include(x => x.Items).ToListAsync();
        return lists
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToList();
    }

    public async Task PatchItemAsync(int itemId, bool? checkedOff, bool? alreadyOwned)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.ShoppingListItems.FirstAsync(x => x.Id == itemId);
        if (checkedOff.HasValue)
        {
            item.CheckedOff = checkedOff.Value;
        }

        if (alreadyOwned.HasValue)
        {
            item.AlreadyOwned = alreadyOwned.Value;
        }

        await db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.ShoppingListItems.FirstOrDefaultAsync(x => x.Id == itemId);
        if (item is null)
        {
            return;
        }

        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync();
    }
}
