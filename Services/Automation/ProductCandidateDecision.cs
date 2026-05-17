namespace ShoppingAgent.Services.Automation;

public sealed record ProductCandidateDecision(
    string ProductName,
    string ProductUrl,
    double Confidence,
    string Reason);
