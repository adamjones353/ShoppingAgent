namespace ShoppingAgent.Services.Automation;

public sealed record ProductMappingCaptureResult(
    bool Succeeded,
    string Message,
    string ProductName = "",
    string ProductUrl = "");
