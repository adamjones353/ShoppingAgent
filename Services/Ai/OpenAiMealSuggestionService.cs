using System.Text.Json;
using System.Text.Json.Serialization;
using ShoppingAgent.Contracts;
using ShoppingAgent.Services.Settings;

namespace ShoppingAgent.Services.Ai;

public sealed class OpenAiMealSuggestionService(
    OpenAiClient client,
    ISettingsService settingsService,
    IAiUsageService usageService) : IOpenAiMealSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<AiSuggestedMealsResponse> SuggestMealsAsync(string prompt, IReadOnlyCollection<string> recentMealNames, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var compactPrompt = $"Request: {prompt}\nAvoid recently eaten meals: {string.Join(", ", recentMealNames.Take(20))}";
        var inputTokens = EstimateTokens(compactPrompt);
        await usageService.EnsureBudgetAsync("MealSuggestions", inputTokens, settings.MaxTokensPerRequest);

        var systemPrompt = """
You suggest practical household meals. Return only JSON in this shape:
{"meals":[{"name":"","description":"","prepEffort":"Low","cookingTimeMinutes":30,"portions":4,"tags":[],"ingredients":[{"name":"","quantity":0,"unit":"","category":""}],"cookingSteps":[]}]}
Use Low, Medium, or High for prepEffort. Keep ingredients concrete and quantities numeric.
""";

        try
        {
            var json = await client.CreateJsonResponseAsync(settings.OpenAiApiKey, settings.OpenAiModel, systemPrompt, compactPrompt, settings.MaxTokensPerRequest, cancellationToken);
            var result = JsonSerializer.Deserialize<AiSuggestedMealsResponse>(json, JsonOptions) ?? new AiSuggestedMealsResponse([]);
            Validate(result);
            await usageService.LogAsync("MealSuggestions", settings.OpenAiModel, inputTokens, EstimateTokens(json), true);
            return result;
        }
        catch (Exception ex)
        {
            await usageService.LogAsync("MealSuggestions", settings.OpenAiModel, inputTokens, 0, false, ex.Message);
            throw;
        }
    }

    private static void Validate(AiSuggestedMealsResponse response)
    {
        foreach (var meal in response.Meals)
        {
            if (string.IsNullOrWhiteSpace(meal.Name) || meal.CookingTimeMinutes <= 0 || meal.Portions <= 0)
            {
                throw new InvalidOperationException("AI returned invalid meal data.");
            }
        }
    }

    private static int EstimateTokens(string value) => Math.Max(1, value.Length / 4);
}
