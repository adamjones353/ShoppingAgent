using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.AppSettings.AnyAsync())
        {
            db.AppSettings.AddRange(
                Setting("OpenAi:ApiKey", ""),
                Setting("OpenAi:Model", "gpt-4.1-mini"),
                Setting("OpenAi:MonthlyBudgetUsd", "10"),
                Setting("OpenAi:DailyBudgetUsd", "1"),
                Setting("OpenAi:MaxTokensPerRequest", "2500"),
                Setting("OpenAi:MaxRetries", "2"),
                Setting("Automation:EnableAiBrowserFallback", "false"),
                Setting("Automation:EnableShoppingAutomation", "false"),
                Setting("Tesco:Email", ""),
                Setting("Tesco:Password", ""));
        }

        if (await db.Meals.AnyAsync())
        {
            await db.SaveChangesAsync();
            return;
        }

        var ingredients = new[]
        {
            Ingredient("chicken breast", "Meat", "g"),
            Ingredient("bacon", "Meat", "g"),
            Ingredient("rice", "Dry goods", "g"),
            Ingredient("pasta", "Dry goods", "g"),
            Ingredient("tinned tomatoes", "Tins", "tin"),
            Ingredient("onion", "Vegetables", "each"),
            Ingredient("garlic", "Vegetables", "clove"),
            Ingredient("broccoli", "Vegetables", "g"),
            Ingredient("cheddar", "Dairy", "g"),
            Ingredient("eggs", "Dairy", "each"),
            Ingredient("wraps", "Bakery", "each"),
            Ingredient("mixed salad", "Vegetables", "bag")
        };
        db.Ingredients.AddRange(ingredients);
        await db.SaveChangesAsync();

        var byName = await db.Ingredients.ToDictionaryAsync(x => x.Name);

        AddMeal(db, byName, "Chicken Fried Rice", "Fast pan rice with chicken, egg and vegetables.", PrepEffort.Low, 25, ["weekday", "quick", "leftovers"], ["Cook rice or use leftovers.", "Fry chicken, onion and garlic.", "Add broccoli and rice.", "Stir through egg until cooked."],
            ("chicken breast", 450m, "g"), ("rice", 300m, "g"), ("eggs", 2m, "each"), ("broccoli", 250m, "g"), ("onion", 1m, "each"));
        AddMeal(db, byName, "Bacon Tomato Pasta", "Simple pasta with smoky bacon and tomato sauce.", PrepEffort.Low, 30, ["weekday", "quick"], ["Boil pasta.", "Fry bacon, onion and garlic.", "Add tomatoes and simmer.", "Toss with pasta and cheddar."],
            ("bacon", 200m, "g"), ("pasta", 350m, "g"), ("tinned tomatoes", 2m, "tin"), ("onion", 1m, "each"), ("cheddar", 80m, "g"));
        AddMeal(db, byName, "Chicken Wraps", "Flexible wraps with salad and cooked chicken.", PrepEffort.Low, 20, ["weekday", "quick"], ["Cook sliced chicken.", "Warm wraps.", "Fill with salad, chicken and cheese."],
            ("chicken breast", 400m, "g"), ("wraps", 8m, "each"), ("mixed salad", 1m, "bag"), ("cheddar", 100m, "g"));
        AddMeal(db, byName, "Batch Chicken Pasta Bake", "Leftover-friendly pasta bake for dinner and lunches.", PrepEffort.Medium, 50, ["batch-cook", "leftovers"], ["Cook pasta.", "Make tomato chicken sauce.", "Combine in baking dish.", "Top with cheddar and bake."],
            ("chicken breast", 650m, "g"), ("pasta", 500m, "g"), ("tinned tomatoes", 3m, "tin"), ("cheddar", 180m, "g"), ("onion", 2m, "each"));

        db.ProductMappings.Add(new ProductMapping
        {
            IngredientId = byName["chicken breast"].Id,
            SupermarketName = "Tesco",
            ProductName = "Tesco British Chicken Breast Fillets",
            SearchTerm = "Tesco British Chicken Breast Fillets",
            PreferredQuantity = "650g"
        });

        await db.SaveChangesAsync();
    }

    private static AppSetting Setting(string key, string value) => new() { Key = key, Value = value };

    private static Ingredient Ingredient(string name, string category, string unit) => new()
    {
        Name = name,
        Category = category,
        DefaultUnit = unit
    };

    private static void AddMeal(AppDbContext db, Dictionary<string, Ingredient> ingredients, string name, string description, PrepEffort effort, int minutes, List<string> tags, List<string> steps, params (string Name, decimal Quantity, string Unit)[] mealIngredients)
    {
        var meal = new Meal
        {
            Name = name,
            Description = description,
            PrepEffort = effort,
            CookingTimeMinutes = minutes,
            Portions = 4,
            Tags = tags,
            CookingSteps = steps,
            Source = MealSource.Preloaded,
            Approved = true
        };

        foreach (var item in mealIngredients)
        {
            meal.Ingredients.Add(new MealIngredient
            {
                IngredientId = ingredients[item.Name].Id,
                Quantity = item.Quantity,
                Unit = item.Unit
            });
        }

        db.Meals.Add(meal);
    }
}
