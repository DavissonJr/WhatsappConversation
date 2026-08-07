using MediatR;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.BulkMessages;

public record GetBulkAudiencePreviewQuery(BulkAudienceFilters Filters) : IRequest<int>;

public class GetBulkAudiencePreviewHandler : IRequestHandler<GetBulkAudiencePreviewQuery, int>
{
    private readonly IApplicationDbContext _db;
    public GetBulkAudiencePreviewHandler(IApplicationDbContext db) => _db = db;

    public async Task<int> Handle(GetBulkAudiencePreviewQuery request, CancellationToken ct)
    {
        var contacts = await BulkAudienceResolver.ResolveAsync(_db, request.Filters, ct);
        return contacts.Count;
    }
}
