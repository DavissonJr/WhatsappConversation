namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Resolve o TenantId atual (via claim do JWT em requests autenticados,
/// ou via header interno em chamadas de webhook do WhatsApp).
/// </summary>
public interface ICurrentTenantService
{
    Guid? TenantId { get; }
}
