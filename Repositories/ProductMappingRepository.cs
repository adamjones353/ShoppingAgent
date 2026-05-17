using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Contracts;
using ShoppingAgent.Data;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Repositories;

public sealed class ProductMappingRepository(IDbContextFactory<AppDbContext> dbFactory) : IProductMappingRepository
{
    public async Task<List<ProductMapping>> GetMappingsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ProductMappings.Include(x => x.Ingredient).OrderBy(x => x.SupermarketName).ThenBy(x => x.ProductName).ToListAsync();
    }

    public async Task<ProductMapping?> GetPreferredMappingAsync(int ingredientId, string supermarketName)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ProductMappings
            .Include(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.IngredientId == ingredientId && x.SupermarketName == supermarketName);
    }

    public async Task<ProductMapping> AddMappingAsync(ProductMappingRequest request)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var mapping = new ProductMapping
        {
            IngredientId = request.IngredientId,
            SupermarketName = request.SupermarketName,
            ProductName = request.ProductName,
            SearchTerm = request.SearchTerm,
            ProductUrl = request.ProductUrl,
            PreferredQuantity = request.PreferredQuantity,
            Notes = request.Notes
        };
        db.ProductMappings.Add(mapping);
        await db.SaveChangesAsync();
        return mapping;
    }

    public async Task<ProductMapping> SavePreferredMappingAsync(ProductMappingRequest request)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var mapping = await db.ProductMappings.FirstOrDefaultAsync(x =>
            x.IngredientId == request.IngredientId &&
            x.SupermarketName == request.SupermarketName);

        if (mapping is null)
        {
            mapping = new ProductMapping
            {
                IngredientId = request.IngredientId,
                SupermarketName = request.SupermarketName,
                ProductName = request.ProductName,
                SearchTerm = request.SearchTerm
            };
            db.ProductMappings.Add(mapping);
        }

        mapping.ProductName = request.ProductName;
        mapping.SearchTerm = request.SearchTerm;
        mapping.ProductUrl = request.ProductUrl;
        mapping.PreferredQuantity = request.PreferredQuantity;
        mapping.Notes = request.Notes;
        mapping.LastUsedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return mapping;
    }
}
