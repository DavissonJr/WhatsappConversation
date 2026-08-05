using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Common;
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
    private readonly INotificationService _notifications;
    private readonly ICurrentTenantService _currentTenant;

    public StartConversationHandler(
        IApplicationDbContext db,
        IWhatsAppGateway whatsApp,
        INotificationService notifications,
        ICurrentTenantService currentTenant)
    {
        _db = db;
        _whatsApp = whatsApp;
        _notifications = notifications;
        _currentTenant = currentTenant;
    }

    public async Task<Guid?> Handle(StartConversationCommand request, CancellationToken ct)
    {
        var connection = await _db.WhatsAppConnections
            .FirstOrDefaultAsync(w => w.Id == request.WhatsAppConnectionId, ct);
        if (connection is null) return null;

        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);

        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedPhone, ct);

        if (contact is null)
        {
            contact = new Contact
            {
                PhoneNumber = normalizedPhone,
                Name = request.ContactName
            };
            _db.Contacts.Add(contact);
        }

        // Contatos já existentes que ainda não têm foto também tentam buscar de novo.
        if (string.IsNullOrEmpty(contact.ProfilePictureUrl))
        {
            try
            {
                contact.ProfilePictureUrl = await _whatsApp.GetProfilePictureUrlAsync(
                    connection.InstanceName, normalizedPhone, ct);
            }
            catch { /* segue sem foto */ }
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

        await _whatsApp.SendTextMessageAsync(connection.InstanceName, normalizedPhone, request.Content, ct);

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

        if (_currentTenant.TenantId is { } tenantId)
            await _notifications.NotifyConversationUpdated(tenantId, conversation.Id);

        return conversation.Id;
    }
}
