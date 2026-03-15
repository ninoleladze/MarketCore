namespace MarketCore.Domain.Common;

public abstract class BaseEntity
{

    public Guid Id { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    internal void SetCreatedAudit(DateTime createdAt, string createdBy)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        UpdatedAt = createdAt;
    }

    internal void SetUpdatedAudit(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }
}
