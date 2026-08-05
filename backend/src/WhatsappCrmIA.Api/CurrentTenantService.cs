using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Lê o TenantId da claim "tenant_id" do usuário autenticado (emitida no login/registro).
/// Para chamadas internas (webhook do WhatsApp), o controller resolve
/// o tenant pelo parâmetro da URL e não depende deste serviço.
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; }

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        var claim = httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        TenantId = Guid.TryParse(claim, out var id) ? id : null;
    }
}
