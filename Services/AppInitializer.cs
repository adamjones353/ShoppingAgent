using Microsoft.EntityFrameworkCore;
using ShoppingAgent.Data;

namespace ShoppingAgent.Services;

public sealed class AppInitializer(IDbContextFactory<AppDbContext> dbFactory) : IAppInitializer
{
    public async Task InitializeAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await SeedData.EnsureSeededAsync(db);
    }
}
