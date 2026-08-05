namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Abstração sobre "avisar o painel que algo mudou em tempo real".
/// Implementada com SignalR na camada Api — a Application não precisa saber disso.
/// </summary>
public interface INotificationService
{
    Task NotifyConversationUpdated(Guid tenantId, Guid conversationId);
}
