using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Contacts;

public record GetContactsQuery(string? Search) : IRequest<IReadOnlyList<ContactListItemDto>>;

public class GetContactsHandler : IRequestHandler<GetContactsQuery, IReadOnlyList<ContactListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetContactsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContactListItemDto>> Handle(GetContactsQuery request, CancellationToken ct)
    {
        var query = _db.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                (c.Name != null && c.Name.ToLower().Contains(term)) ||
                c.PhoneNumber.Contains(term));
        }

        var contacts = await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct);
        var contactIds = contacts.Select(c => c.Id).ToList();

        var conversationInfo = await _db.Conversations
            .Where(c => contactIds.Contains(c.ContactId))
            .GroupBy(c => c.ContactId)
            .Select(g => new { ContactId = g.Key, Count = g.Count(), LastAt = g.Max(c => c.LastMessageAtUtc) })
            .ToDictionaryAsync(x => x.ContactId, x => x, ct);

        var appointmentCounts = await _db.Appointments
            .Where(a => contactIds.Contains(a.ContactId))
            .GroupBy(a => a.ContactId)
            .Select(g => new { ContactId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ContactId, x => x.Count, ct);

        var proposalCounts = await _db.Proposals
            .Where(p => contactIds.Contains(p.ContactId))
            .GroupBy(p => p.ContactId)
            .Select(g => new { ContactId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ContactId, x => x.Count, ct);

        return contacts.Select(c =>
        {
            conversationInfo.TryGetValue(c.Id, out var conv);
            return new ContactListItemDto(
                c.Id, c.Name, c.PhoneNumber, c.ProfilePictureUrl, c.Notes, c.IsBlocked, c.CreatedAtUtc,
                conv?.LastAt, conv?.Count ?? 0,
                appointmentCounts.GetValueOrDefault(c.Id, 0),
                proposalCounts.GetValueOrDefault(c.Id, 0));
        }).ToList();
    }
}
