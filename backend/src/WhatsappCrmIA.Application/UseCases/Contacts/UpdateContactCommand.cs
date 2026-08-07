using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Contacts;

public record UpdateContactCommand(Guid ContactId, string? Name, string? Notes, bool IsBlocked) : IRequest<bool>;

public class UpdateContactHandler : IRequestHandler<UpdateContactCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public UpdateContactHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateContactCommand request, CancellationToken ct)
    {
        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == request.ContactId, ct);
        if (contact is null) return false;

        contact.Name = request.Name;
        contact.Notes = request.Notes;
        contact.IsBlocked = request.IsBlocked;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
