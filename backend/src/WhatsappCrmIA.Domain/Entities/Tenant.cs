using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Representa a empresa cliente do SaaS (clínica, oficina, escritório, imobiliária...).
/// Nota: Tenant não herda de BaseEntity pois ele é a raiz do isolamento multi-tenant.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Segment { get; set; } = default!; // ex: "clinica", "oficina", "advocacia", "imobiliaria"
    public PlanTier Plan { get; set; } = PlanTier.Trial;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Config do agente de IA para este tenant
    public AiAgentConfig? AiAgentConfig { get; set; }

    // Um tenant pode conectar vários números de WhatsApp
    public ICollection<WhatsAppConnection> WhatsAppConnections { get; set; } = new List<WhatsAppConnection>();
}
