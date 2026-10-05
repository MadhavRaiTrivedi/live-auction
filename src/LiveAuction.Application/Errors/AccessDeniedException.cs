namespace LiveAuction.Application.Errors;

public sealed class AccessDeniedException(string message) : Exception(message);
