using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.UseCases.Messaging;

namespace WhatsappCrmIA.Api.Controllers;

/// <summary>
/// Recebe os eventos da Evolution API (mensagens recebidas via WhatsApp).
/// Configure esta URL como webhook da instância: POST /webhook/evolution/{tenantId}
/// </summary>
[ApiController]
[Route("webhook/evolution")]
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhookController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{tenantId:guid}")]
    public async Task<IActionResult> ReceiveMessage(Guid tenantId, [FromBody] EvolutionWebhookPayload payload)
    {
        // A Evolution API dispara vários tipos de evento (connection.update, messages.upsert, etc).
        // Aqui tratamos apenas mensagens de texto recebidas (simplificado para o MVP).
        if (payload.Event != "messages.upsert" || payload.Data?.Message?.Conversation is null)
            return Ok();

        if (payload.Data.Key?.FromMe == true)
            return Ok(); // ignora mensagens enviadas pelo próprio número

        var result = await _mediator.Send(new ProcessIncomingMessageCommand(
            TenantId: tenantId,
            FromPhoneNumber: payload.Data.Key?.RemoteJid?.Split('@').FirstOrDefault() ?? string.Empty,
            ContactName: payload.Data.PushName ?? "Contato",
            MessageText: payload.Data.Message.Conversation,
            WhatsAppMessageId: payload.Data.Key?.Id ?? string.Empty
        ));

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
