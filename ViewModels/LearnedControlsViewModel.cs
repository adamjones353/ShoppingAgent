using System.Collections.ObjectModel;
using ShoppingAgent.Domain;
using ShoppingAgent.Services.Automation;

namespace ShoppingAgent.ViewModels;

public sealed class LearnedControlsViewModel(ILearnedControlService controls) : ViewModelBase, ILoadableViewModel
{
    public ObservableCollection<LearnedBrowserControl> Controls { get; } = [];

    public async Task LoadAsync()
    {
        Controls.Clear();
        foreach (var control in await controls.GetControlsAsync())
        {
            Controls.Add(control);
        }
    }
}
