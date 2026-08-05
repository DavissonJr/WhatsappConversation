using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.UseCases.Messaging;

namespace WhatsappCrmIA.Api.Controllers;

/// <summary>
/// Recebe os eventos da Evolution API (mensagens recebidas via WhatsApp).
/// Configure esta URL como webhook da instância: POST /webhook/evolution/{tenantId}/{instanceName}
/// </summary>
[ApiController]
[Route("webhook/evolution")]
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IMediator mediator, ILogger<WebhookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("{tenantId:guid}/{instanceName}")]
    public async Task<IActionResult> ReceiveMessage(
        Guid tenantId, string instanceName, [FromBody] EvolutionWebhookPayload payload)
    {
        _logger.LogInformation(
            "Webhook recebido: tenant={TenantId} instance={InstanceName} event={Event}",
            tenantId, instanceName, payload.Event);

        // A Evolution API dispara vários tipos de evento (connection.update, messages.upsert, etc).
        // Aqui tratamos apenas mensagens de texto recebidas (simplificado para o MVP).
        if (payload.Event != "messages.upsert" || payload.Data?.Message?.Conversation is null)
        {
            _logger.LogInformation(
                "Webhook ignorado (evento={Event}, tem texto={TemText})",
                payload.Event, payload.Data?.Message?.Conversation is not null);
            return Ok();
        }

        if (payload.Data.Key?.FromMe == true)
        {
            _logger.LogInformation("Webhook ignorado: mensagem enviada por nós mesmos (fromMe=true)");
            return Ok();
        }

        var result = await _mediator.Send(new ProcessIncomingMessageCommand(
            TenantId: tenantId,
            InstanceName: instanceName,
            FromPhoneNumber: payload.Data.Key?.RemoteJid?.Split('@').FirstOrDefault() ?? string.Empty,
            ContactName: payload.Data.PushName ?? "Contato",
            MessageText: payload.Data.Message.Conversation,
            WhatsAppMessageId: payload.Data.Key?.Id ?? string.Empty
        ));

        _logger.LogInformation(
            "Resultado do processamento: autoRespondeu={AutoReplied}",
            result.AutoReplied);

        return Ok(result);
    }
}

// DTOs simplificados do payload da Evolution API — ajustar conforme a versão usada.
public class EvolutionWebhookPayload
{
    public string? Event { get; set; }
    public EvolutionData? Data { get; set; }
}

public class EvolutionData
{
    public EvolutionKey? Key { get; set; }
    public string? PushName { get; set; }
    public EvolutionMessage? Message { get; set; }
}

public class EvolutionKey
{
    public string? RemoteJid { get; set; }
    public bool FromMe { get; set; }
    public string? Id { get; set; }
}

public class EvolutionMessage
{
    public string? Conversation { get; set; }
}
