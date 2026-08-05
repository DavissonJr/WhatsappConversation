using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Proposals;

public record GetProposalsQuery : IRequest<IReadOnlyList<ProposalDto>>;

public class GetProposalsHandler : IRequestHandler<GetProposalsQuery, IReadOnlyList<ProposalDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProposalsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProposalDto>> Handle(GetProposalsQuery request, CancellationToken ct)
    {
        var proposals = await _db.Proposals
            .Include(p => p.Contact)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        return proposals
            .Select(p => new ProposalDto(
                p.Id,
                new ContactDto(p.Contact.Id, p.Contact.Name, p.Contact.PhoneNumber, p.Contact.ProfilePictureUrl),
                p.ConversationId,
                p.Title,
                p.Description,
                p.Value,
                p.Status.ToString(),
                p.AiGenerated,
                p.SentAtUtc,
                p.CreatedAtUtc))
            .ToList();
    }
}
