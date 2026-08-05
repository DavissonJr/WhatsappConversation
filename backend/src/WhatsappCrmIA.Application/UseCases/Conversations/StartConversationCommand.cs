using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Conversations;

/// <summary>
/// Usado pelo painel quando o atendente quer mandar a primeira mensagem
/// para um número que ainda não escreveu (ex: prospecção, cobrança ativa).
/// </summary>
public record StartConversationCommand(
    Guid WhatsAppConnectionId,
    string PhoneNumber,
    string? ContactName,
    string Content
) : IRequest<Guid?>;

public class StartConversationHandler : IRequestHandler<StartConversationCommand, Guid?>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;

    public StartConversationHandler(IApplicationDbContext db, IWhatsAppGateway whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    public async Task<Guid?> Handle(StartConversationCommand request, CancellationToken ct)
    {
        var connection = await _db.WhatsAppConnections
            .FirstOrDefaultAsync(w => w.Id == request.WhatsAppConnectionId, ct);
        if (connection is null) return null;

        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber, ct);

        if (contact is null)
        {
            contact = new Contact
            {
                PhoneNumber = request.PhoneNumber,
                Name = request.ContactName
            };
            _db.Contacts.Add(contact);
        }

        var conversation = await _db.Conversations
            .Where(c => c.ContactId == contact.Id
                        && c.WhatsAppConnectionId == connection.Id
                        && c.Status != ConversationStatus.Closed)
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                Contact = contact,
                WhatsAppConnectionId = connection.Id,
                Status = ConversationStatus.Open
            };
            _db.Conversations.Add(conversation);
        }

        await _whatsApp.SendTextMessageAsync(connection.InstanceName, request.PhoneNumber, request.Content, ct);

        _db.Messages.Add(new Message
        {
            Conversation = conversation,
            Content = request.Content,
            Direction = MessageDirection.Outbound,
            SentBy = MessageSender.HumanAgent,
            AiGenerated = false
        });

        conversation.LastMessageAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return conversation.Id;
    }
}
