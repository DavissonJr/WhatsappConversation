namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Monta a URL de webhook que a Evolution API vai chamar quando uma mensagem chegar.
/// Implementado na Infrastructure porque depende de configuração (host interno da API).
/// </summary>
public interface IWebhookUrlBuilder
{
    string Build(Guid tenantId, string instanceName);
}
