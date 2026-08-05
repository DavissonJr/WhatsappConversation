using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Conversations;

public record GetConversationsQuery : IRequest<IReadOnlyList<ConversationSummaryDto>>;

public class GetConversationsHandler
    : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetConversationsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConversationSummaryDto>> Handle(
        GetConversationsQuery request, CancellationToken ct)
    {
        // O global query filter do EF Core já restringe ao TenantId atual.
        var conversations = await _db.Conversations
            .Include(c => c.Contact)
            .Include(c => c.Messages)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToListAsync(ct);

        return conversations
            .Select(c => new ConversationSummaryDto(
                c.Id,
                new ContactDto(c.Contact.Id, c.Contact.Name, c.Contact.PhoneNumber),
                c.Status.ToString(),
                c.LastMessageAtUtc,
                c.Messages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault()?.Content))
            .ToList();
    }
}
