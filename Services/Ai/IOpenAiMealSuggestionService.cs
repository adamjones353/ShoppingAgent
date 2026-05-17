using ShoppingAgent.Contracts;

namespace ShoppingAgent.Services.Ai;

public interface IOpenAiMealSuggestionService
{
    Task<AiSuggestedMealsResponse> SuggestMealsAsync(string prompt, IReadOnlyCollection<string> recentMealNames, CancellationToken cancellationToken = default);
}
