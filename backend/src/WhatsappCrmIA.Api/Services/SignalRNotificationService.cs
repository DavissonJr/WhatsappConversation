using Microsoft.AspNetCore.SignalR;
using WhatsappCrmIA.Api.Hubs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Api.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<ConversationHub> _hub;

    public SignalRNotificationService(IHubContext<ConversationHub> hub) => _hub = hub;

    public Task NotifyConversationUpdated(Guid tenantId, Guid conversationId) =>
        _hub.Clients.Group(tenantId.ToString()).SendAsync("conversationUpdated", conversationId);
}
