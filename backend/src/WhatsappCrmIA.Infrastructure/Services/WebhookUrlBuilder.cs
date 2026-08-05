using Microsoft.Extensions.Configuration;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Constrói a URL de webhook usando o host interno da API dentro da rede Docker
/// (ex: "http://api:8080"), configurado em "Api:InternalBaseUrl".
/// </summary>
public class WebhookUrlBuilder : IWebhookUrlBuilder
{
    private readonly IConfiguration _config;

    public WebhookUrlBuilder(IConfiguration config) => _config = config;

    public string Build(Guid tenantId, string instanceName)
    {
        var baseUrl = _config["Api:InternalBaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Api:InternalBaseUrl não configurado.");

        return $"{baseUrl}/webhook/evolution/{tenantId}/{instanceName}";
    }
}
