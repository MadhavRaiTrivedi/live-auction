namespace LiveAuction.Application.Errors;

public sealed class InvalidRequestException(string message) : Exception(message);
