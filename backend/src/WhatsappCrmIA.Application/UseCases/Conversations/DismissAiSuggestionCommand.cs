using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Conversations;

public record DismissAiSuggestionCommand(Guid ConversationId) : IRequest<bool>;

public class DismissAiSuggestionHandler : IRequestHandler<DismissAiSuggestionCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public DismissAiSuggestionHandler(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<bool> Handle(DismissAiSuggestionCommand request, CancellationToken ct)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

        if (conversation is null) return false;

        conversation.PendingAiSuggestion = null;
        if (conversation.Status == ConversationStatus.WaitingHuman)
            conversation.Status = ConversationStatus.Open;

        await _db.SaveChangesAsync(ct);
        await _notifications.NotifyConversationUpdated(conversation.TenantId, conversation.Id);
        return true;
    }
}
