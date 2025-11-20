namespace ArtemisBanking.Core.Domain.Primitives;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    public void SetUpdatedNow()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}