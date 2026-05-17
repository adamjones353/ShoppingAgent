using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Repositories;

public interface IProductMappingRepository
{
    Task<List<ProductMapping>> GetMappingsAsync();
    Task<ProductMapping?> GetPreferredMappingAsync(int ingredientId, string supermarketName);
    Task<ProductMapping> AddMappingAsync(ProductMappingRequest request);
    Task<ProductMapping> SavePreferredMappingAsync(ProductMappingRequest request);
}
