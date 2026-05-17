namespace ShoppingAgent.ViewModels;

public sealed record NavigationItem(string Title, Func<ViewModelBase> Factory);
