namespace RandWise.Domain.Common;

public abstract class UserOwnedAggregateRoot : Entity
{
    protected UserOwnedAggregateRoot(string id, string userId)
        : base(id)
    {
        UserId = DomainGuard.Required(userId, nameof(UserId), 128);
    }

    protected UserOwnedAggregateRoot()
    {
        UserId = string.Empty;
    }

    public string UserId { get; protected set; }
}
