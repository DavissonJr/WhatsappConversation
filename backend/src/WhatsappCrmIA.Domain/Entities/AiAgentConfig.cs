using WhatsappCrmIA.Domain.Common;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Configuração do agente de IA por tenant: persona, tom, regras de negócio,
/// e se a resposta deve ser enviada automaticamente ou aguardar aprovação humana.
/// </summary>
public class AiAgentConfig : BaseEntity
{
    public string AgentName { get; set; } = "Assistente Virtual";
    public string SystemPrompt { get; set; } = default!;
    public bool AutoReplyEnabled { get; set; } = true;
    public bool RequireHumanApproval { get; set; } = false;
    public string BusinessHours { get; set; } = "08:00-18:00"; // simplificado para o MVP
    public string? FallbackMessage { get; set; } = "Já te chamo, um momento!";

    /// <summary>
    /// Chave da API da Anthropic do PRÓPRIO tenant, guardada criptografada
    /// (nunca em texto puro). Cada empresa usa a conta e o saldo dela na
    /// Anthropic — o custo da IA não sai da conta de quem administra o SaaS.
    /// </summary>
    public string? AnthropicApiKeyEncrypted { get; set; }

    /// <summary>
    /// Só os últimos 4 caracteres da chave, para exibir algo tipo "sk-ant-...ab12"
    /// no painel sem nunca expor a chave completa de novo depois de salva.
    /// </summary>
    public string? AnthropicApiKeyPreview { get; set; }
}
