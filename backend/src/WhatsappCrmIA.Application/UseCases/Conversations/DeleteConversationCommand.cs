using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Conversations;

public record DeleteConversationCommand(Guid ConversationId) : IRequest<bool>;

public class DeleteConversationHandler : IRequestHandler<DeleteConversationCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public DeleteConversationHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteConversationCommand request, CancellationToken ct)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

        if (conversation is null) return false;

        _db.Conversations.Remove(conversation);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
