using Microsoft.Playwright;
using System.Diagnostics;
using System.IO;
using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;
using ShoppingAgent.Services.Settings;

namespace ShoppingAgent.Services.Automation;

public sealed class TescoAutomationService(
    ILearnedControlService learnedControls,
    IBrowserAiFallbackService aiFallback,
    ISettingsService settingsService) : IBrowserAutomationService
{
    private IPlaywright? _mappingPlaywright;
    private IBrowser? _mappingBrowser;
    private IPage? _mappingPage;
    private IPlaywright? _shoppingPlaywright;
    private IBrowser? _shoppingBrowser;
    private IPage? _shoppingPage;
    private IPlaywright? _manualPlaywright;
    private IBrowser? _manualBrowser;
    private IPage? _manualPage;
    private Process? _manualChromeProcess;

    public async Task<BrowserActionResult> OpenTescoLoginInDefaultBrowserAsync()
    {
        OpenUrlInNormalBrowser("https://www.tesco.com/account/en-GB/login");
        return await Task.FromResult(new BrowserActionResult(true, "Opened Tesco login in your normal browser. Log in manually there, then return to the app."));
    }

    public async Task<BrowserActionResult> OpenDeliverySlotInDefaultBrowserAsync()
    {
        OpenUrlInNormalBrowser("https://www.tesco.com/groceries/en-GB/slots");
        return await Task.FromResult(new BrowserActionResult(true, "Opened Tesco delivery slots in your normal browser. Pick a slot manually, then return to the app."));
    }

    public async Task<BrowserActionResult> OpenShoppingItemInDefaultBrowserAsync(string searchTerm, string productUrl = "")
    {
        var url = string.IsNullOrWhiteSpace(productUrl)
            ? $"https://www.tesco.com/groceries/en-GB/search?query={Uri.EscapeDataString(searchTerm)}"
            : productUrl;
        OpenUrlInNormalBrowser(url);
        return await Task.FromResult(new BrowserActionResult(true, $"Opened Tesco for {searchTerm} in your normal browser. If this opens a new tab, copy the URL shown in the app into your existing Tesco tab."));
    }

    private static void OpenUrlInNormalBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public async Task<ProductMappingCaptureResult> CaptureOpenProductPageAsync()
    {
        if (_manualPage is null)
        {
            return new ProductMappingCaptureResult(false, "ShoppingAgent Chrome tab is not open.");
        }

        if (!_manualPage.Url.Contains("/products/", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductMappingCaptureResult(false, "Open a Tesco product page in the ShoppingAgent Chrome tab first.");
        }

        return await CaptureProductFromPageAsync(_manualPage, "Open product page captured.");
    }

    public async Task<BrowserActionResult> AddOpenProductPageToBasketAsync(CancellationToken cancellationToken = default)
    {
        if (_manualPage is null)
        {
            return new BrowserActionResult(false, "ShoppingAgent Chrome tab is not open.");
        }

        var addButton = await ResolveAddToBasketButtonAsync(_manualPage);
        if (addButton is null)
        {
            return new BrowserActionResult(false, "I could not find the add-to-basket button on the open product page.");
        }

        await addButton.ClickAsync();
        return new BrowserActionResult(true, "Added open product page to basket.");
    }

    public async Task<BrowserActionResult> SearchProductAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (!settings.EnableShoppingAutomation)
        {
            return new BrowserActionResult(false, "Shopping automation is disabled in settings.");
        }

        IPlaywright playwright;
        IBrowser browser;
        try
        {
            var launch = await LaunchVisibleBrowserAsync();
            if (!launch.Succeeded)
            {
                return new BrowserActionResult(false, launch.Message);
            }

            playwright = launch.Playwright!;
            browser = launch.Browser!;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            return new BrowserActionResult(false, PlaywrightInstallMessage);
        }

        using var playwrightScope = playwright;
        await using var browserScope = browser;
        var page = await NewVisiblePageAsync(browser);
        await page.GotoAsync("https://www.tesco.com/groceries/en-GB/");
        await AcceptCookieConsentAsync(page);

        var input = await ResolveLocatorAsync(page, "Search Tesco groceries", "ProductSearchInput", cancellationToken);
        if (input is null)
        {
            return new BrowserActionResult(false, "I can't find the search box. Please click it once in teaching mode.");
        }

        await input.FillAsync(searchTerm);
        await RememberSearchInputAsync(page);
        await page.Keyboard.PressAsync("Enter");
        return new BrowserActionResult(true, $"Opened Tesco search results for {searchTerm}. Review manually before adding anything to basket.");
    }

    public async Task<BrowserActionResult> StartProductMappingAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (!settings.EnableShoppingAutomation)
        {
            return new BrowserActionResult(false, "Shopping automation is disabled in settings.");
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new BrowserActionResult(false, "Enter or select an ingredient/search term first.");
        }

        await CloseMappingSessionAsync();
        try
        {
            var launch = await LaunchVisibleBrowserAsync();
            if (!launch.Succeeded)
            {
                return new BrowserActionResult(false, launch.Message);
            }

            _mappingPlaywright = launch.Playwright;
            _mappingBrowser = launch.Browser;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            _mappingPlaywright?.Dispose();
            _mappingPlaywright = null;
            return new BrowserActionResult(false, PlaywrightInstallMessage);
        }

        _mappingPage = await NewVisiblePageAsync(_mappingBrowser);
        await _mappingPage.GotoAsync("https://www.tesco.com/groceries/en-GB/");
        await AcceptCookieConsentAsync(_mappingPage);

        var input = await ResolveLocatorAsync(_mappingPage, "Search Tesco groceries for preferred product mapping", "ProductSearchInput", cancellationToken);
        if (input is null)
        {
            return new BrowserActionResult(false, "I can't find the search box. Please click it once in teaching mode.");
        }

        await input.FillAsync(searchTerm);
        await RememberSearchInputAsync(_mappingPage);
        await _mappingPage.Keyboard.PressAsync("Enter");
        return new BrowserActionResult(true, $"Tesco search is open for {searchTerm}. Choose the product in the browser, then click Confirm Current Product.");
    }

    public async Task<ProductMappingCaptureResult> CaptureCurrentProductAsync(CancellationToken cancellationToken = default)
    {
        if (_mappingPage is null)
        {
            return new ProductMappingCaptureResult(false, "No active Tesco mapping browser session.");
        }

        var url = _mappingPage.Url;
        if (!url.Contains("tesco.com", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductMappingCaptureResult(false, "The active page is not a Tesco product page.");
        }

        if (!url.Contains("/products/", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductMappingCaptureResult(false, "Open a specific Tesco product page first, then confirm it.");
        }

        var productName = await TryReadProductNameAsync(_mappingPage);
        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = await _mappingPage.TitleAsync();
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            return new ProductMappingCaptureResult(false, "I could not read the product name from the current page.");
        }

        return new ProductMappingCaptureResult(true, "Captured current Tesco product.", CleanTitle(productName), url);
    }

    public async Task<BrowserActionResult> StartShoppingSessionAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (!settings.EnableShoppingAutomation)
        {
            return new BrowserActionResult(false, "Shopping automation is disabled in settings.");
        }

        await CloseShoppingSessionAsync();
        var launch = await LaunchVisibleBrowserAsync();
        if (!launch.Succeeded)
        {
            return new BrowserActionResult(false, launch.Message);
        }

        _shoppingPlaywright = launch.Playwright;
        _shoppingBrowser = launch.Browser;
        _shoppingPage = await NewVisiblePageAsync(_shoppingBrowser!);
        await _shoppingPage.GotoAsync("https://www.tesco.com/account/en-GB/login");
        await AcceptCookieConsentAsync(_shoppingPage);
        await ShowBrowserPanelAsync(_shoppingPage, "Log in to Tesco manually, then return to the app and click Pick Delivery Slot.");
        return new BrowserActionResult(true, "Chrome opened on Tesco login. Log in manually, then click Pick Delivery Slot.");
    }

    public async Task<BrowserActionResult> LoginToTescoAsync(CancellationToken cancellationToken = default)
    {
        if (_shoppingPage is null)
        {
            var started = await StartShoppingSessionAsync(cancellationToken);
            if (!started.Succeeded)
            {
                return started;
            }
        }

        if (_shoppingPage is null)
        {
            return new BrowserActionResult(false, "Shopping browser is not available.");
        }

        await _shoppingPage.GotoAsync("https://www.tesco.com/account/en-GB/login");
        await AcceptCookieConsentAsync(_shoppingPage);
        await ShowBrowserPanelAsync(_shoppingPage, "Log in to Tesco manually, then return to the app and click Pick Delivery Slot.");
        return new BrowserActionResult(true, "Tesco login opened. Log in manually in Chrome, then click Pick Delivery Slot.");
    }

    public async Task<BrowserActionResult> OpenDeliverySlotPageAsync(CancellationToken cancellationToken = default)
    {
        if (_shoppingPage is null)
        {
            var started = await StartShoppingSessionAsync(cancellationToken);
            if (!started.Succeeded)
            {
                return started;
            }
        }

        if (_shoppingPage is null)
        {
            return new BrowserActionResult(false, "Shopping browser is not available.");
        }

        await _shoppingPage.GotoAsync("https://www.tesco.com/groceries/en-GB/slots");
        await AcceptCookieConsentAsync(_shoppingPage);
        await ShowBrowserPanelAsync(_shoppingPage, "Pick a delivery slot manually, then return to the app and click Resume Shopping.");
        return new BrowserActionResult(true, "Delivery slot page opened. Pick a slot in the browser, then click Resume Shopping.");
    }

    public async Task<BrowserActionResult> ResumeAfterDeliverySlotAsync(CancellationToken cancellationToken = default)
    {
        if (_shoppingPage is null)
        {
            return new BrowserActionResult(false, "Shopping browser is not open.");
        }

        await _shoppingPage.GotoAsync("https://www.tesco.com/groceries/en-GB/");
        await AcceptCookieConsentAsync(_shoppingPage);
        await ShowBrowserPanelAsync(_shoppingPage, "Shopping resumed. The app will search each item and ask before adding to basket.");
        return new BrowserActionResult(true, "Resumed shopping after delivery slot selection. Click Next Item to start finding products.");
    }

    public async Task<ProductMappingCaptureResult> OpenShoppingItemAsync(string searchTerm, string productUrl = "", CancellationToken cancellationToken = default)
    {
        if (_shoppingPage is null)
        {
            var started = await StartShoppingSessionAsync(cancellationToken);
            if (!started.Succeeded)
            {
                return new ProductMappingCaptureResult(false, started.Message);
            }
        }

        if (_shoppingPage is null)
        {
            return new ProductMappingCaptureResult(false, "Shopping browser is not available.");
        }

        if (!string.IsNullOrWhiteSpace(productUrl))
        {
            await _shoppingPage.GotoAsync(productUrl);
            await AcceptCookieConsentAsync(_shoppingPage);
            await ShowBrowserPanelAsync(_shoppingPage, "Preferred product opened. Confirm in the app before adding to basket.");
            return await CaptureProductFromPageAsync(_shoppingPage, "Opened preferred product.");
        }

        await _shoppingPage.GotoAsync("https://www.tesco.com/groceries/en-GB/");
        await AcceptCookieConsentAsync(_shoppingPage);
        await ShowBrowserPanelAsync(_shoppingPage, $"Searching for {searchTerm}.");
        var input = await ResolveLocatorAsync(_shoppingPage, "Search Tesco groceries for shopping item", "ProductSearchInput", cancellationToken);
        if (input is null)
        {
            return new ProductMappingCaptureResult(false, "I can't find the Tesco search box.");
        }

        await input.FillAsync(searchTerm);
        await RememberSearchInputAsync(_shoppingPage);
        await _shoppingPage.Keyboard.PressAsync("Enter");
        await _shoppingPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        try
        {
            var opened = await TryOpenFirstProductResultAsync(_shoppingPage, searchTerm, cancellationToken);
            if (!opened)
            {
                return new ProductMappingCaptureResult(false, $"I could not identify a product result for {searchTerm}. Try choosing one manually in the browser.");
            }
        }
        catch (TimeoutException)
        {
            return new ProductMappingCaptureResult(false, $"Timed out opening a Tesco result for {searchTerm}.");
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductMappingCaptureResult(false, $"Timed out opening a Tesco result for {searchTerm}.");
        }

        var capture = await CaptureProductFromPageAsync(_shoppingPage, $"Found candidate for {searchTerm}.");
        if (!capture.Succeeded || !capture.ProductUrl.Contains("/products/", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductMappingCaptureResult(false, $"I did not reach a product page for {searchTerm}. Choose a product manually or enable AI browser fallback.");
        }

        return capture;
    }

    public async Task<BrowserActionResult> AddCurrentProductToBasketAsync(CancellationToken cancellationToken = default)
    {
        if (_shoppingPage is null)
        {
            return new BrowserActionResult(false, "Shopping browser is not open.");
        }

        var addButton = await ResolveAddToBasketButtonAsync(_shoppingPage);
        if (addButton is null)
        {
            return new BrowserActionResult(false, "I could not find the add-to-basket button. Please add it manually in the browser.");
        }

        await addButton.ClickAsync();
        await ShowBrowserPanelAsync(_shoppingPage, "Product added. The app will move to the next item.");
        return new BrowserActionResult(true, "Added current product to basket.");
    }

    public async Task<BrowserActionResult> StopShoppingSessionAsync()
    {
        await CloseShoppingSessionAsync();
        return new BrowserActionResult(true, "Shopping automation stopped.");
    }

    public async Task<ProductMappingCaptureResult> WaitForManualProductSelectionAsync(CancellationToken cancellationToken = default)
    {
        if (_shoppingPage is null)
        {
            return new ProductMappingCaptureResult(false, "Shopping browser is not open.");
        }

        var deadline = DateTimeOffset.UtcNow.AddMinutes(10);
        while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            if (_shoppingPage.Url.Contains("/products/", StringComparison.OrdinalIgnoreCase))
            {
                return await CaptureProductFromPageAsync(_shoppingPage, "Manual product selection detected.");
            }

            await Task.Delay(1000, cancellationToken);
        }

        return new ProductMappingCaptureResult(false, "Timed out waiting for a manual product selection.");
    }

    public async Task<BrowserActionResult> OpenMappedProductAsync(string productUrl, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (!settings.EnableShoppingAutomation)
        {
            return new BrowserActionResult(false, "Shopping automation is disabled in settings.");
        }

        if (string.IsNullOrWhiteSpace(productUrl))
        {
            return new BrowserActionResult(false, "The product mapping has no URL.");
        }

        IPlaywright playwright;
        IBrowser browser;
        try
        {
            var launch = await LaunchVisibleBrowserAsync();
            if (!launch.Succeeded)
            {
                return new BrowserActionResult(false, launch.Message);
            }

            playwright = launch.Playwright!;
            browser = launch.Browser!;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            return new BrowserActionResult(false, PlaywrightInstallMessage);
        }

        using var playwrightScope = playwright;
        await using var browserScope = browser;
        var page = await NewVisiblePageAsync(browser);
        await page.GotoAsync(productUrl);
        await AcceptCookieConsentAsync(page);
        return new BrowserActionResult(true, "Opened mapped Tesco product. Basket and checkout remain manual.");
    }

    private async Task<ILocator?> ResolveLocatorAsync(IPage page, string currentTask, string purpose, CancellationToken cancellationToken)
    {
        foreach (var control in await learnedControls.FindCandidatesAsync("Tesco", purpose, page.Url))
        {
            var locator = CreateLocator(page, control);
            if (await locator.CountAsync() > 0)
            {
                await learnedControls.MarkSuccessAsync(control.Id);
                return locator.First;
            }

            await learnedControls.MarkFailureAsync(control.Id);
        }

        var accessibilityCandidates = new[]
        {
            page.GetByRole(AriaRole.Searchbox),
            page.GetByPlaceholder("Search"),
            page.GetByLabel("Search"),
            page.GetByText("Search", new PageGetByTextOptions { Exact = false })
        };

        foreach (var candidate in accessibilityCandidates)
        {
            if (await candidate.CountAsync() > 0)
            {
                return candidate.First;
            }
        }

        var css = page.Locator("input[type='search'], input[name*='search' i], input[placeholder*='search' i]");
        if (await css.CountAsync() > 0)
        {
            return css.First;
        }

        var decision = await aiFallback.ResolveControlAsync(page, currentTask, purpose, cancellationToken);
        if (decision is null || decision.Action == "none" || decision.Confidence < 0.65 || string.IsNullOrWhiteSpace(decision.LocatorValue))
        {
            return null;
        }

        var locatorType = decision.LocatorStrategy.ToLowerInvariant() switch
        {
            "placeholder" => LocatorType.Placeholder,
            "label" => LocatorType.Label,
            "text" => LocatorType.Text,
            _ => LocatorType.Css
        };

        var aiLocator = locatorType switch
        {
            LocatorType.Placeholder => page.GetByPlaceholder(decision.LocatorValue),
            LocatorType.Label => page.GetByLabel(decision.LocatorValue),
            LocatorType.Text => page.GetByText(decision.LocatorValue),
            _ => page.Locator(decision.LocatorValue)
        };

        if (await aiLocator.CountAsync() == 0)
        {
            return null;
        }

        await learnedControls.SaveSuccessAsync(new LearnedControlRequest("Tesco", "Search", purpose, locatorType, decision.LocatorValue, "", "", "tesco.com/groceries"), decision.Confidence);
        return aiLocator.First;
    }

    private static ILocator CreateLocator(IPage page, LearnedBrowserControl control) => control.LocatorType switch
    {
        LocatorType.Role when Enum.TryParse<AriaRole>(control.AccessibleRole, true, out var role) => page.GetByRole(role, new PageGetByRoleOptions { Name = control.AccessibleName }),
        LocatorType.Label => page.GetByLabel(control.LocatorValue),
        LocatorType.Placeholder => page.GetByPlaceholder(control.LocatorValue),
        LocatorType.Text => page.GetByText(control.LocatorValue),
        LocatorType.XPath => page.Locator($"xpath={control.LocatorValue}"),
        _ => page.Locator(control.LocatorValue)
    };

    private async Task RememberSearchInputAsync(IPage page)
    {
        await learnedControls.SaveSuccessAsync(new LearnedControlRequest(
            "Tesco",
            "Search",
            "ProductSearchInput",
            LocatorType.Css,
            "input[type='search'], input[name*='search' i], input[placeholder*='search' i]",
            "Searchbox",
            "Search",
            "tesco.com/groceries"), 0.75);
    }

    private static async Task<string> TryReadProductNameAsync(IPage page)
    {
        var candidates = new[]
        {
            page.Locator("h1").First,
            page.GetByRole(AriaRole.Heading).First,
            page.Locator("[data-auto='product-tile--title'], [data-testid*='product-title']").First
        };

        foreach (var candidate in candidates)
        {
            if (await candidate.CountAsync() > 0)
            {
                var text = await candidate.InnerTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        return "";
    }

    private static async Task<ProductMappingCaptureResult> CaptureProductFromPageAsync(IPage page, string successMessage)
    {
        if (!page.Url.Contains("tesco.com", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductMappingCaptureResult(false, "The browser is not on Tesco.");
        }

        var productName = await TryReadProductNameAsync(page);
        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = await page.TitleAsync();
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            return new ProductMappingCaptureResult(false, "I could not read a candidate product name.");
        }

        return new ProductMappingCaptureResult(true, successMessage, CleanTitle(productName), page.Url);
    }

    private async Task<bool> TryOpenFirstProductResultAsync(IPage page, string searchTerm, CancellationToken cancellationToken)
    {
        var candidates = await page.Locator("a[href*='/products/']")
            .EvaluateAllAsync<string[]>(
                """
                elements => elements
                  .map(e => e.href || e.getAttribute('href') || '')
                  .filter(h => h.includes('tesco.com') && h.includes('/products/') && !h.includes('onetrust.com') && !h.includes('cookie-consent'))
                  .filter((h, i, all) => all.indexOf(h) === i)
                  .slice(0, 5)
                """);

        foreach (var href in candidates)
        {
            if (!TryGetTescoProductUri(href, out var uri))
            {
                continue;
            }

            await page.GotoAsync(uri.ToString(), new PageGotoOptions { Timeout = 15000, WaitUntil = WaitUntilState.DOMContentLoaded });
            return true;
        }

        var aiCandidate = await aiFallback.ChooseProductCandidateAsync(page, searchTerm, cancellationToken);
        if (aiCandidate is null || aiCandidate.Confidence < 0.55 || !TryGetTescoProductUri(aiCandidate.ProductUrl, out var aiUri))
        {
            return false;
        }

        await page.GotoAsync(aiUri.ToString(), new PageGotoOptions { Timeout = 15000, WaitUntil = WaitUntilState.DOMContentLoaded });
        return true;
    }

    private async Task<ILocator?> ResolveAddToBasketButtonAsync(IPage page)
    {
        foreach (var control in await learnedControls.FindCandidatesAsync("Tesco", "AddToBasketButton", page.Url))
        {
            var locator = CreateLocator(page, control);
            if (await locator.CountAsync() > 0)
            {
                await learnedControls.MarkSuccessAsync(control.Id);
                return locator.First;
            }

            await learnedControls.MarkFailureAsync(control.Id);
        }

        var candidates = new[]
        {
            page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add" }),
            page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add to basket" }),
            page.GetByText("Add to basket", new PageGetByTextOptions { Exact = false }),
            page.Locator("button:has-text('Add')")
        };

        foreach (var candidate in candidates)
        {
            if (await candidate.CountAsync() > 0)
            {
                await learnedControls.SaveSuccessAsync(new LearnedControlRequest(
                    "Tesco",
                    "Product",
                    "AddToBasketButton",
                    LocatorType.Text,
                    "Add",
                    "Button",
                    "Add",
                    "tesco.com/groceries"), 0.72);
                return candidate.First;
            }
        }

        return null;
    }

    private static async Task AcceptCookieConsentAsync(IPage page)
    {
        var candidates = new[]
        {
            page.Locator("#onetrust-accept-btn-handler").First,
            page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Accept all cookies" }),
            page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Accept all" }),
            page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Accept" }),
            page.Locator("button:has-text('Accept all')").First,
            page.Locator("button:has-text('Accept')").First
        };

        foreach (var candidate in candidates)
        {
            try
            {
                if (await candidate.CountAsync() > 0 && await candidate.First.IsVisibleAsync())
                {
                    await candidate.First.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                    await page.WaitForTimeoutAsync(500);
                    return;
                }
            }
            catch (PlaywrightException)
            {
                // Cookie banners vary and can disappear while we are checking them.
            }
        }
    }

    private static async Task ShowBrowserPanelAsync(IPage page, string message)
    {
        var safeMessage = message.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
        await page.EvaluateAsync(
            $$"""
            () => {
              let panel = document.getElementById('shopping-agent-panel');
              if (!panel) {
                panel = document.createElement('div');
                panel.id = 'shopping-agent-panel';
                panel.style.position = 'fixed';
                panel.style.right = '16px';
                panel.style.bottom = '16px';
                panel.style.zIndex = '2147483647';
                panel.style.maxWidth = '360px';
                panel.style.padding = '12px 14px';
                panel.style.background = '#111827';
                panel.style.color = '#f8fafc';
                panel.style.border = '2px solid #5DBB8A';
                panel.style.borderRadius = '8px';
                panel.style.boxShadow = '0 8px 28px rgba(0,0,0,.35)';
                panel.style.font = '14px Segoe UI, Arial, sans-serif';
                document.body.appendChild(panel);
              }
              panel.textContent = `Shopping Agent: {{safeMessage}}`;
            }
            """);
    }

    private static async Task<IPage> NewVisiblePageAsync(IBrowser browser)
    {
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
        return page;
    }

    private static string CleanTitle(string title)
    {
        var value = title.Replace("| Tesco", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" - Tesco Groceries", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return value.Length > 200 ? value[..200] : value;
    }

    private static bool TryGetTescoProductUri(string href, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(href, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!parsed.Host.Contains("tesco.com", StringComparison.OrdinalIgnoreCase) ||
            !parsed.AbsolutePath.Contains("/products/", StringComparison.OrdinalIgnoreCase) ||
            parsed.Host.Contains("onetrust", StringComparison.OrdinalIgnoreCase) ||
            parsed.AbsoluteUri.Contains("cookie-consent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static async Task<bool> TryFillFirstAsync(IPage page, string value, params ILocator[] locators)
    {
        foreach (var locator in locators)
        {
            try
            {
                if (await locator.CountAsync() > 0)
                {
                    await locator.First.FillAsync(value);
                    return true;
                }
            }
            catch (PlaywrightException)
            {
                // Try the next candidate. Labels can match multiple controls on Tesco pages.
            }
        }

        return false;
    }

    private static async Task<ILocator?> FirstExistingAsync(params ILocator[] locators)
    {
        foreach (var locator in locators)
        {
            if (await locator.CountAsync() > 0)
            {
                return locator.First;
            }
        }

        return null;
    }

    private async Task CloseMappingSessionAsync()
    {
        if (_mappingBrowser is not null)
        {
            await _mappingBrowser.DisposeAsync();
        }

        _mappingPlaywright?.Dispose();
        _mappingBrowser = null;
        _mappingPage = null;
        _mappingPlaywright = null;
    }

    private async Task CloseShoppingSessionAsync()
    {
        if (_shoppingBrowser is not null)
        {
            await _shoppingBrowser.DisposeAsync();
        }

        _shoppingPlaywright?.Dispose();
        _shoppingBrowser = null;
        _shoppingPage = null;
        _shoppingPlaywright = null;
    }

    private async Task<BrowserActionResult> NavigateManualChromeAsync(string url)
    {
        var page = await GetOrCreateManualChromePageAsync(url);
        if (page is null)
        {
            return new BrowserActionResult(false, "Could not open Chrome. Install Google Chrome and try again.");
        }

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
        await AcceptCookieConsentAsync(page);
        await ShowBrowserPanelAsync(page, "Use this same tab for the whole shopping flow.");
        return new BrowserActionResult(true, "Chrome navigated.");
    }

    private async Task<IPage?> GetOrCreateManualChromePageAsync(string initialUrl)
    {
        if (_manualPage is not null && !_manualPage.IsClosed)
        {
            return _manualPage;
        }

        var chromePath = FindChromePath();
        if (chromePath is null)
        {
            return null;
        }

        var userDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShoppingAgent",
            "ChromeProfile");
        Directory.CreateDirectory(userDataDir);

        _manualChromeProcess ??= Process.Start(new ProcessStartInfo
        {
            FileName = chromePath,
            Arguments = $"--remote-debugging-port=9222 --user-data-dir=\"{userDataDir}\" --start-maximized --new-window \"{initialUrl}\"",
            UseShellExecute = false
        });

        _manualPlaywright ??= await Playwright.CreateAsync();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                _manualBrowser ??= await _manualPlaywright.Chromium.ConnectOverCDPAsync("http://127.0.0.1:9222");
                var context = _manualBrowser.Contexts.FirstOrDefault();
                _manualPage = context?.Pages.FirstOrDefault(x => !x.IsClosed) ?? await context!.NewPageAsync();
                return _manualPage;
            }
            catch (PlaywrightException)
            {
                await Task.Delay(500);
            }
        }

        return null;
    }

    private static string? FindChromePath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            "chrome.exe"
        };

        return candidates.FirstOrDefault(path =>
            path.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase) || File.Exists(path));
    }

    private static async Task<BrowserLaunchResult> LaunchVisibleBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();
        foreach (var channel in new[] { "chrome", "" })
        {
            try
            {
                var options = string.IsNullOrWhiteSpace(channel)
                    ? new BrowserTypeLaunchOptions
                    {
                        Headless = false,
                        Args = ["--start-maximized"]
                    }
                    : new BrowserTypeLaunchOptions
                    {
                        Headless = false,
                        Channel = channel,
                        Args = ["--start-maximized"]
                    };
                var browser = await playwright.Chromium.LaunchAsync(options);
                return new BrowserLaunchResult(true, "", playwright, browser);
            }
            catch (PlaywrightException ex) when (
                ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
        }

        playwright.Dispose();
        return new BrowserLaunchResult(false, PlaywrightInstallMessage, null, null);
    }

    private const string PlaywrightInstallMessage =
        "Chrome was not found. Install Google Chrome or open Automation Settings and click Install Chromium.";

    private sealed record BrowserLaunchResult(bool Succeeded, string Message, IPlaywright? Playwright, IBrowser? Browser);
}
