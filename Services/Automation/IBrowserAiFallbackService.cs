using Microsoft.Playwright;

namespace ShoppingAgent.Services.Automation;

public interface IBrowserAiFallbackService
{
    Task<BrowserAiFallbackDecision?> ResolveControlAsync(IPage page, string currentTask, string purpose, CancellationToken cancellationToken);
    Task<ProductCandidateDecision?> ChooseProductCandidateAsync(IPage page, string shoppingItemName, CancellationToken cancellationToken);
}
