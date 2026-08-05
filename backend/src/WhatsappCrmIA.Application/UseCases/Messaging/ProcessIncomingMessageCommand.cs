using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Messaging;

/// <summary>
/// Fluxo central do produto: mensagem chega do webhook da Evolution API,
/// é persistida, a IA gera (ou sugere) uma resposta, e opcionalmente
/// já dispara o envio de volta ao contato.
/// </summary>
public record ProcessIncomingMessageCommand(
    Guid TenantId,
    string InstanceName,
    string FromPhoneNumber,
    string ContactName,
    string MessageText,
    string WhatsAppMessageId
) : IRequest<ProcessIncomingMessageResult>;

public record ProcessIncomingMessageResult(bool AutoReplied, string? ReplyText);

public class ProcessIncomingMessageHandler
    : IRequestHandler<ProcessIncomingMessageCommand, ProcessIncomingMessageResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IAiAgentService _aiAgent;
    private readonly IWhatsAppGateway _whatsApp;

    public ProcessIncomingMessageHandler(
        IApplicationDbContext db,
        IAiAgentService aiAgent,
        IWhatsAppGateway whatsApp)
    {
        _db = db;
        _aiAgent = aiAgent;
        _whatsApp = whatsApp;
    }

    public async Task<ProcessIncomingMessageResult> Handle(
        ProcessIncomingMessageCommand request, CancellationToken ct)
    {
        // 0. Resolve qual número da empresa recebeu essa mensagem
        var whatsappConnection = await _db.WhatsAppConnections
            .FirstOrDefaultAsync(w => w.TenantId == request.TenantId
                                       && w.InstanceName == request.InstanceName, ct);

        if (whatsappConnection is null)
            return new ProcessIncomingMessageResult(false, null);

        // 1. Garante o contato
        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId
                                       && c.PhoneNumber == request.FromPhoneNumber, ct);

        if (contact is null)
        {
            contact = new Contact
            {
                TenantId = request.TenantId,
                PhoneNumber = request.FromPhoneNumber,
                Name = request.ContactName
            };
            _db.Contacts.Add(contact);
        }

        // 2. Garante a conversa aberta (ligada a esse contato E a esse número específico)
        var conversation = await _db.Conversations
            .Include(c => c.Messages)
            .Where(c => c.ContactId == contact.Id
                        && c.WhatsAppConnectionId == whatsappConnection.Id
                        && c.Status != ConversationStatus.Closed)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                TenantId = request.TenantId,
                Contact = contact,
                WhatsAppConnectionId = whatsappConnection.Id,
                Status = ConversationStatus.Open
            };
            _db.Conversations.Add(conversation);
        }

        // 3. Persiste a mensagem recebida
        var inboundMessage = new Message
        {
            TenantId = request.TenantId,
            Conversation = conversation,
            Content = request.MessageText,
            Direction = MessageDirection.Inbound,
            SentBy = MessageSender.Contact,
            WhatsAppMessageId = request.WhatsAppMessageId
        };
        _db.Messages.Add(inboundMessage);
        conversation.LastMessageAtUtc = DateTime.UtcNow;

        // 4. Busca config do agente de IA do tenant
        var agentConfig = await _db.AiAgentConfigs
            .FirstOrDefaultAsync(a => a.TenantId == request.TenantId, ct);

        if (agentConfig is null || !agentConfig.AutoReplyEnabled)
        {
            await _db.SaveChangesAsync(ct);
            return new ProcessIncomingMessageResult(false, null);
        }

        // 5. Monta histórico e chama a IA (Claude)
        var history = conversation.Messages
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => (
                role: m.Direction == MessageDirection.Inbound ? "user" : "assistant",
                content: m.Content))
            .ToList();
        history.Add(("user", request.MessageText));

        var aiResult = await _aiAgent.GenerateReplyAsync(agentConfig.SystemPrompt, history, ct);
        conversation.LastDetectedIntent = aiResult.DetectedIntent;

        // 6. Se precisa de aprovação humana, apenas registra sugestão e para por aqui.
        //    (No painel, o agente humano aprova e um outro comando dispara o envio.)
        if (agentConfig.RequireHumanApproval || aiResult.ShouldEscalateToHuman)
        {
            conversation.Status = ConversationStatus.WaitingHuman;
            await _db.SaveChangesAsync(ct);
            return new ProcessIncomingMessageResult(false, aiResult.ReplyText);
        }

        // 7. Envia a resposta automaticamente
        await _whatsApp.SendTextMessageAsync(
            whatsappConnection.InstanceName, request.FromPhoneNumber, aiResult.ReplyText, ct);

        _db.Messages.Add(new Message
        {
            TenantId = request.TenantId,
            Conversation = conversation,
            Content = aiResult.ReplyText,
            Direction = MessageDirection.Outbound,
            SentBy = MessageSender.AiAgent,
            AiGenerated = true
        });

        await _db.SaveChangesAsync(ct);
        return new ProcessIncomingMessageResult(true, aiResult.ReplyText);
    }
}
