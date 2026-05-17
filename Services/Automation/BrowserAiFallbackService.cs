using System.Text.Json;
using Microsoft.Playwright;
using ShoppingAgent.Services.Ai;
using ShoppingAgent.Services.Settings;

namespace ShoppingAgent.Services.Automation;

public sealed class BrowserAiFallbackService(
    OpenAiClient client,
    ISettingsService settingsService,
    IAiUsageService usageService) : IBrowserAiFallbackService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<BrowserAiFallbackDecision?> ResolveControlAsync(IPage page, string currentTask, string purpose, CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (!settings.EnableAiBrowserFallback || string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            return null;
        }

        var candidates = await page.Locator("input, button, [role], a").EvaluateAllAsync<string[]>(
            """
            elements => elements.slice(0, 80).map((e, i) => {
              const text = (e.innerText || e.getAttribute('aria-label') || e.getAttribute('placeholder') || e.getAttribute('name') || '').trim();
              return `${i}: ${e.tagName.toLowerCase()} role=${e.getAttribute('role') || ''} text=${text.slice(0,80)}`;
            })
            """);

        var prompt = JsonSerializer.Serialize(new
        {
            currentTask,
            purpose,
            url = page.Url,
            title = await page.TitleAsync(),
            candidateElements = candidates
        });

        var inputTokens = Math.Max(1, prompt.Length / 4);
        await usageService.EnsureBudgetAsync("BrowserControlFallback", inputTokens, 600);

        var systemPrompt = """
Choose one browser control for Playwright. Return only JSON:
{"action":"click|fill|none","purpose":"","locatorStrategy":"css|text|placeholder|label","locatorValue":"","confidence":0,"reason":""}
Prefer stable accessibility-visible selectors. Do not invent selectors.
""";

        try
        {
            var json = await client.CreateJsonResponseAsync(settings.OpenAiApiKey, settings.OpenAiModel, systemPrompt, prompt, 600, cancellationToken);
            await usageService.LogAsync("BrowserControlFallback", settings.OpenAiModel, inputTokens, Math.Max(1, json.Length / 4), true);
            return JsonSerializer.Deserialize<BrowserAiFallbackDecision>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            await usageService.LogAsync("BrowserControlFallback", settings.OpenAiModel, inputTokens, 0, false, ex.Message);
            return null;
        }
    }

    public async Task<ProductCandidateDecision?> ChooseProductCandidateAsync(IPage page, string shoppingItemName, CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (!settings.EnableAiBrowserFallback || string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            return null;
        }

        var compactHtml = await page.Locator("body").EvaluateAsync<string>(
            """
            body => {
              const clone = body.cloneNode(true);
              clone.querySelectorAll('script,style,svg,img,iframe,noscript').forEach(e => e.remove());
              clone.querySelectorAll('[style]').forEach(e => e.removeAttribute('style'));
              clone.querySelectorAll('[class]').forEach(e => e.setAttribute('class', String(e.getAttribute('class') || '').slice(0, 80)));
              const html = clone.innerHTML.replace(/\s+/g, ' ').slice(0, 30000);
              const productLinks = Array.from(clone.querySelectorAll('a[href*="/products/"]')).slice(0, 120).map(a => ({
                text: (a.innerText || a.getAttribute('aria-label') || '').trim().slice(0, 240),
                href: a.href || a.getAttribute('href') || ''
              }));
              return JSON.stringify({ html, productLinks });
            }
            """);

        var prompt = JsonSerializer.Serialize(new
        {
            task = "Choose the best Tesco product result for a shopping-list item using the supplied sanitized page HTML.",
            shoppingItemName,
            url = page.Url,
            title = await page.TitleAsync(),
            compactHtml
        });

        var inputTokens = Math.Max(1, prompt.Length / 4);
        await usageService.EnsureBudgetAsync("ProductCandidateFallback", inputTokens, 700);

        var systemPrompt = """
Pick one product URL from the provided Tesco sanitized HTML. Return only JSON:
{"productName":"","productUrl":"","confidence":0,"reason":""}
Only use URLs present in the snapshot. If no good product exists, return confidence 0 and blank URL.
Choose good value for money: prefer sensible mid/low priced items, avoid suspiciously tiny/ultra-cheap products unless they clearly match the shopping item, and avoid sponsored/irrelevant substitutes.
""";

        try
        {
            var json = await client.CreateJsonResponseAsync(settings.OpenAiApiKey, settings.OpenAiModel, systemPrompt, prompt, 700, cancellationToken);
            await usageService.LogAsync("ProductCandidateFallback", settings.OpenAiModel, inputTokens, Math.Max(1, json.Length / 4), true);
            return JsonSerializer.Deserialize<ProductCandidateDecision>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            await usageService.LogAsync("ProductCandidateFallback", settings.OpenAiModel, inputTokens, 0, false, ex.Message);
            return null;
        }
    }
}
