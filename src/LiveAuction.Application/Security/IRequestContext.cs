namespace LiveAuction.Application.Security;

public interface IRequestContext
{
    Requester Requester { get; }
}
