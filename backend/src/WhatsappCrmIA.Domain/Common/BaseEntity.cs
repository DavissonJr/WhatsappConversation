namespace WhatsappCrmIA.Domain.Common;

/// <summary>
/// Entidade base. Todas as entidades multi-tenant carregam TenantId
/// para isolamento de dados via global query filter no EF Core.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
