namespace LiveAuction.Domain;

public sealed class DomainRuleViolationException(DomainErrorCode code, string message) : Exception(message)
{
    public DomainErrorCode Code { get; } = code;
}
