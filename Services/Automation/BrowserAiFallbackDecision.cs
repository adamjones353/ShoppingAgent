namespace ShoppingAgent.Services.Automation;

public sealed record BrowserAiFallbackDecision(
    string Action,
    string Purpose,
    string LocatorStrategy,
    string LocatorValue,
    double Confidence,
    string Reason);
