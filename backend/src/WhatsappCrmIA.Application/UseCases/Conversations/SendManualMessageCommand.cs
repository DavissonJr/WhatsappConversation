using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Conversations;

/// <summary>
/// Usado pelo painel (Inbox) quando um atendente humano responde manualmente
/// — seja uma conversa comum, seja uma que a IA escalou (status WaitingHuman).
/// </summary>
public record SendManualMessageCommand(Guid ConversationId, string Content) : IRequest<bool>;

public class SendManualMessageHandler : IRequestHandler<SendManualMessageCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;

    public SendManualMessageHandler(IApplicationDbContext db, IWhatsAppGateway whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    public async Task<bool> Handle(SendManualMessageCommand request, CancellationToken ct)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Contact)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

        if (conversation is null) return false;

        var connection = await _db.WhatsAppConnections
            .FirstOrDefaultAsync(w => w.Id == conversation.WhatsAppConnectionId, ct);

        if (connection is null) return false;

        await _whatsApp.SendTextMessageAsync(
            connection.InstanceName, conversation.Contact.PhoneNumber, request.Content, ct);

        _db.Messages.Add(new Message
        {
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Content = request.Content,
            Direction = MessageDirection.Outbound,
            SentBy = MessageSender.HumanAgent,
            AiGenerated = false
        });

        conversation.LastMessageAtUtc = DateTime.UtcNow;
        // Se a conversa estava esperando um humano, volta para "Open" após a resposta.
        if (conversation.Status == ConversationStatus.WaitingHuman)
            conversation.Status = ConversationStatus.Open;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
