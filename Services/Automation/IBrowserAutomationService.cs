namespace ShoppingAgent.Services.Automation;

public interface IBrowserAutomationService
{
    Task<BrowserActionResult> SearchProductAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<BrowserActionResult> OpenMappedProductAsync(string productUrl, CancellationToken cancellationToken = default);
    Task<BrowserActionResult> StartProductMappingAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<ProductMappingCaptureResult> CaptureCurrentProductAsync(CancellationToken cancellationToken = default);
    Task<BrowserActionResult> StartShoppingSessionAsync(CancellationToken cancellationToken = default);
    Task<ProductMappingCaptureResult> OpenShoppingItemAsync(string searchTerm, string productUrl = "", CancellationToken cancellationToken = default);
    Task<BrowserActionResult> AddCurrentProductToBasketAsync(CancellationToken cancellationToken = default);
    Task<BrowserActionResult> StopShoppingSessionAsync();
    Task<BrowserActionResult> LoginToTescoAsync(CancellationToken cancellationToken = default);
    Task<BrowserActionResult> OpenDeliverySlotPageAsync(CancellationToken cancellationToken = default);
    Task<BrowserActionResult> ResumeAfterDeliverySlotAsync(CancellationToken cancellationToken = default);
    Task<ProductMappingCaptureResult> WaitForManualProductSelectionAsync(CancellationToken cancellationToken = default);
    Task<BrowserActionResult> OpenTescoLoginInDefaultBrowserAsync();
    Task<BrowserActionResult> OpenDeliverySlotInDefaultBrowserAsync();
    Task<BrowserActionResult> OpenShoppingItemInDefaultBrowserAsync(string searchTerm, string productUrl = "");
    Task<ProductMappingCaptureResult> CaptureOpenProductPageAsync();
    Task<BrowserActionResult> AddOpenProductPageToBasketAsync(CancellationToken cancellationToken = default);
}
