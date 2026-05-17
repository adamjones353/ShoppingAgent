using ShoppingAgent.Contracts;
using ShoppingAgent.Domain;

namespace ShoppingAgent.Services.Automation;

public interface ILearnedControlService
{
    Task<List<LearnedBrowserControl>> GetControlsAsync();
    Task<List<LearnedBrowserControl>> FindCandidatesAsync(string siteName, string purpose, string url);
    Task SaveSuccessAsync(LearnedControlRequest request, double confidence);
    Task MarkSuccessAsync(int controlId);
    Task MarkFailureAsync(int controlId);
}
