namespace RandWise.Domain.Common;

public abstract class Entity
{
    protected Entity(string id)
    {
        Id = DomainGuard.Required(id, nameof(Id), 128);
    }

    protected Entity()
    {
        Id = string.Empty;
    }

    public string Id { get; protected set; }
}
