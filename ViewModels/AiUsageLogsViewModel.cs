using System.Collections.ObjectModel;
using ShoppingAgent.Domain;
using ShoppingAgent.Services.Ai;

namespace ShoppingAgent.ViewModels;

public sealed class AiUsageLogsViewModel(IAiUsageService usageService) : ViewModelBase, ILoadableViewModel
{
    public ObservableCollection<AiUsageLog> Logs { get; } = [];

    public async Task LoadAsync()
    {
        Logs.Clear();
        foreach (var log in await usageService.GetRecentLogsAsync())
        {
            Logs.Add(log);
        }
    }
}
