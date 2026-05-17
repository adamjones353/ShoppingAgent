using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShoppingAgent.Data;

#nullable disable

namespace ShoppingAgent.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260517193000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AiUsageLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Purpose = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Model = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                EstimatedInputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                EstimatedOutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AiUsageLogs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                Key = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AppSettings", x => x.Key));

        migrationBuilder.CreateTable(
            name: "Ingredients",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                DefaultUnit = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Ingredients", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LearnedBrowserControls",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                SiteName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                PageType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Purpose = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                LocatorType = table.Column<string>(type: "TEXT", nullable: false),
                LocatorValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                AccessibleRole = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                AccessibleName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                UrlPattern = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                LastSuccessfulUse = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                FailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LearnedBrowserControls", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Meals",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                CookingSteps = table.Column<string>(type: "TEXT", nullable: false),
                PrepEffort = table.Column<string>(type: "TEXT", nullable: false),
                CookingTimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                Portions = table.Column<int>(type: "INTEGER", nullable: false),
                Tags = table.Column<string>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", nullable: false),
                Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Meals", x => x.Id));

        migrationBuilder.CreateTable(
            name: "WeeklyMealPlans",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                WeekStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_WeeklyMealPlans", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ProductMappings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                SupermarketName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                SearchTerm = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ProductUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                PreferredQuantity = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                LastUsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductMappings", x => x.Id);
                table.ForeignKey("FK_ProductMappings_Ingredients_IngredientId", x => x.IngredientId, "Ingredients", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MealHistory",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                MealId = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Rating = table.Column<int>(type: "INTEGER", nullable: true),
                WouldHaveAgain = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MealHistory", x => x.Id);
                table.ForeignKey("FK_MealHistory_Meals_MealId", x => x.MealId, "Meals", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MealIngredients",
            columns: table => new
            {
                MealId = table.Column<int>(type: "INTEGER", nullable: false),
                IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                Unit = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Optional = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MealIngredients", x => new { x.MealId, x.IngredientId });
                table.ForeignKey("FK_MealIngredients_Ingredients_IngredientId", x => x.IngredientId, "Ingredients", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_MealIngredients_Meals_MealId", x => x.MealId, "Meals", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PlannedMeals",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                WeeklyMealPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Slot = table.Column<string>(type: "TEXT", nullable: false),
                MealId = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlannedMeals", x => x.Id);
                table.ForeignKey("FK_PlannedMeals_Meals_MealId", x => x.MealId, "Meals", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_PlannedMeals_WeeklyMealPlans_WeeklyMealPlanId", x => x.WeeklyMealPlanId, "WeeklyMealPlans", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ShoppingLists",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                WeeklyMealPlanId = table.Column<int>(type: "INTEGER", nullable: true),
                Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShoppingLists", x => x.Id);
                table.ForeignKey("FK_ShoppingLists_WeeklyMealPlans_WeeklyMealPlanId", x => x.WeeklyMealPlanId, "WeeklyMealPlans", "Id");
            });

        migrationBuilder.CreateTable(
            name: "ShoppingListItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ShoppingListId = table.Column<int>(type: "INTEGER", nullable: false),
                IngredientId = table.Column<int>(type: "INTEGER", nullable: true),
                Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                Unit = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                CheckedOff = table.Column<bool>(type: "INTEGER", nullable: false),
                AlreadyOwned = table.Column<bool>(type: "INTEGER", nullable: false),
                ManualItem = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShoppingListItems", x => x.Id);
                table.ForeignKey("FK_ShoppingListItems_Ingredients_IngredientId", x => x.IngredientId, "Ingredients", "Id");
                table.ForeignKey("FK_ShoppingListItems_ShoppingLists_ShoppingListId", x => x.ShoppingListId, "ShoppingLists", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Ingredients_Name", "Ingredients", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_MealHistory_MealId", "MealHistory", "MealId");
        migrationBuilder.CreateIndex("IX_MealIngredients_IngredientId", "MealIngredients", "IngredientId");
        migrationBuilder.CreateIndex("IX_PlannedMeals_MealId", "PlannedMeals", "MealId");
        migrationBuilder.CreateIndex("IX_PlannedMeals_WeeklyMealPlanId_Date_Slot", "PlannedMeals", new[] { "WeeklyMealPlanId", "Date", "Slot" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductMappings_IngredientId", "ProductMappings", "IngredientId");
        migrationBuilder.CreateIndex("IX_ShoppingListItems_IngredientId", "ShoppingListItems", "IngredientId");
        migrationBuilder.CreateIndex("IX_ShoppingListItems_ShoppingListId", "ShoppingListItems", "ShoppingListId");
        migrationBuilder.CreateIndex("IX_ShoppingLists_WeeklyMealPlanId", "ShoppingLists", "WeeklyMealPlanId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AiUsageLogs");
        migrationBuilder.DropTable("AppSettings");
        migrationBuilder.DropTable("LearnedBrowserControls");
        migrationBuilder.DropTable("MealHistory");
        migrationBuilder.DropTable("MealIngredients");
        migrationBuilder.DropTable("PlannedMeals");
        migrationBuilder.DropTable("ProductMappings");
        migrationBuilder.DropTable("ShoppingListItems");
        migrationBuilder.DropTable("Meals");
        migrationBuilder.DropTable("ShoppingLists");
        migrationBuilder.DropTable("Ingredients");
        migrationBuilder.DropTable("WeeklyMealPlans");
    }
}
