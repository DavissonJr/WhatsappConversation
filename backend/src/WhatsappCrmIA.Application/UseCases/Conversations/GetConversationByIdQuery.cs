using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Conversations;

public record GetConversationByIdQuery(Guid ConversationId) : IRequest<ConversationDetailDto?>;

public class GetConversationByIdHandler
    : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto?>
{
    private readonly IApplicationDbContext _db;

    public GetConversationByIdHandler(IApplicationDbContext db) => _db = db;

    public async Task<ConversationDetailDto?> Handle(
        GetConversationByIdQuery request, CancellationToken ct)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Contact)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

        if (conversation is null) return null;

        return new ConversationDetailDto(
            conversation.Id,
            new ContactDto(conversation.Contact.Id, conversation.Contact.Name, conversation.Contact.PhoneNumber),
            conversation.Status.ToString(),
            conversation.LastMessageAtUtc,
            conversation.Messages
                .OrderBy(m => m.CreatedAtUtc)
                .Select(m => new MessageDto(
                    m.Id, m.Content, m.Direction.ToString(), m.SentBy.ToString(),
                    m.AiGenerated, m.CreatedAtUtc))
                .ToList());
    }
}
