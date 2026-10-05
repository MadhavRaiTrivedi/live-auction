namespace LiveAuction.Application.Errors;

public sealed class NotFoundException(string resource, object id)
    : Exception($"{resource} {id} was not found.")
{
    public string Resource { get; } = resource;
}
