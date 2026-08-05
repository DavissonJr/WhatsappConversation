using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Api.Services;

/// <summary>
/// Executado pelo Hangfire na hora exata do lembrete. Roda sem nenhum usuário
/// logado (é um job de fundo), então usa IgnoreQueryFilters() nas consultas —
/// mesma lógica do WebhookController.
/// </summary>
public class SendReminderJob
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly INotificationService _notifications;
    private readonly ILogger<SendReminderJob> _logger;

    public SendReminderJob(
        IApplicationDbContext db,
        IWhatsAppGateway whatsApp,
        INotificationService notifications,
        ILogger<SendReminderJob> logger)
    {
        _db = db;
        _whatsApp = whatsApp;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid reminderId)
    {
        var reminder = await _db.Reminders
            .IgnoreQueryFilters()
            .Include(r => r.Appointment).ThenInclude(a => a.Contact)
            .Include(r => r.Appointment).ThenInclude(a => a.WhatsAppConnection)
            .FirstOrDefaultAsync(r => r.Id == reminderId);

        if (reminder is null)
        {
            _logger.LogWarning("Lembrete {ReminderId} não encontrado (pode ter sido apagado).", reminderId);
            return;
        }

        // Segurança: se por algum motivo já foi cancelado/enviado, não faz nada de novo.
        if (reminder.Status != ReminderStatus.Pending) return;

        var appointment = reminder.Appointment;
        var contact = appointment.Contact;
        var connection = appointment.WhatsAppConnection;

        var message = reminder.MessageTemplate
            .Replace("{nome}", contact.Name ?? contact.PhoneNumber)
            .Replace("{titulo}", appointment.Title)
            .Replace("{data}", appointment.ScheduledForUtc.ToString("dd/MM/yyyy"))
            .Replace("{hora}", appointment.ScheduledForUtc.ToString("HH:mm"));

        try
        {
            await _whatsApp.SendTextMessageAsync(connection.InstanceName, contact.PhoneNumber, message);
            reminder.Status = ReminderStatus.Sent;

            // Registra a mensagem na conversa também, pra ficar visível no Inbox
            // como qualquer outra mensagem enviada.
            var conversation = await _db.Conversations
                .IgnoreQueryFilters()
                .Where(c => c.ContactId == contact.Id
                            && c.WhatsAppConnectionId == connection.Id
                            && c.Status != ConversationStatus.Closed)
                .OrderByDescending(c => c.LastMessageAtUtc)
                .FirstOrDefaultAsync();

            if (conversation is null)
            {
                conversation = new Conversation
                {
                    TenantId = reminder.TenantId,
                    ContactId = contact.Id,
                    WhatsAppConnectionId = connection.Id,
                    Status = ConversationStatus.Open
                };
                _db.Conversations.Add(conversation);
            }

            conversation.LastMessageAtUtc = DateTime.UtcNow;
            _db.Messages.Add(new Message
            {
                TenantId = reminder.TenantId,
                Conversation = conversation,
                Content = message,
                Direction = MessageDirection.Outbound,
                SentBy = MessageSender.System,
                AiGenerated = false
            });

            await _db.SaveChangesAsync();
            await _notifications.NotifyConversationUpdated(reminder.TenantId, conversation.Id);

            _logger.LogInformation("Lembrete {ReminderId} enviado com sucesso.", reminderId);
        }
        catch (Exception ex)
        {
            reminder.Status = ReminderStatus.Failed;
            await _db.SaveChangesAsync();
            _logger.LogError(ex, "Falha ao enviar lembrete {ReminderId}.", reminderId);
        }
    }
}
