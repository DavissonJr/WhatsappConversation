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

    /// <summary>
    /// Saldo de créditos (em USD) disponível para chamadas de IA. Cada resposta
    /// gerada consome uma fração desse saldo, calculada pelo uso real de tokens.
    /// Quando chega a zero, a IA para de responder automaticamente (a conversa
    /// só fica esperando um atendente humano — nada quebra).
    /// </summary>
    public decimal AiCreditsBalanceUsd { get; set; } = 5.00m;

    // Config do agente de IA para este tenant
    public AiAgentConfig? AiAgentConfig { get; set; }

    // Um tenant pode conectar vários números de WhatsApp
    public ICollection<WhatsAppConnection> WhatsAppConnections { get; set; } = new List<WhatsAppConnection>();
}
