using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<MealIngredient> MealIngredients => Set<MealIngredient>();
    public DbSet<MealHistoryEntry> MealHistory => Set<MealHistoryEntry>();
    public DbSet<WeeklyMealPlan> WeeklyMealPlans => Set<WeeklyMealPlan>();
    public DbSet<PlannedMeal> PlannedMeals => Set<PlannedMeal>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ProductMapping> ProductMappings => Set<ProductMapping>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<LearnedBrowserControl> LearnedBrowserControls => Set<LearnedBrowserControl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringListConverter = new ValueConverter<List<string>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>());

        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
            value => JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new List<string>());

        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(x => x.Tags).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
            entity.Property(x => x.CookingSteps).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
            entity.Property(x => x.PrepEffort).HasConversion<string>();
            entity.Property(x => x.Source).HasConversion<string>();
        });

        modelBuilder.Entity<MealIngredient>().HasKey(x => new { x.MealId, x.IngredientId });
        modelBuilder.Entity<MealIngredient>()
            .HasOne(x => x.Meal)
            .WithMany(x => x.Ingredients)
            .HasForeignKey(x => x.MealId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MealIngredient>()
            .HasOne(x => x.Ingredient)
            .WithMany()
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlannedMeal>().Property(x => x.Slot).HasConversion<string>();
        modelBuilder.Entity<PlannedMeal>()
            .HasIndex(x => new { x.WeeklyMealPlanId, x.Date, x.Slot })
            .IsUnique();

        modelBuilder.Entity<AppSetting>().HasKey(x => x.Key);
        modelBuilder.Entity<LearnedBrowserControl>().Property(x => x.LocatorType).HasConversion<string>();
        modelBuilder.Entity<Ingredient>().HasIndex(x => x.Name).IsUnique();
    }
}
