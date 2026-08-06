using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Admin;

public record SetTenantActiveCommand(Guid TenantId, bool IsActive) : IRequest<bool>;

public class SetTenantActiveHandler : IRequestHandler<SetTenantActiveCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public SetTenantActiveHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(SetTenantActiveCommand request, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, ct);
        if (tenant is null) return false;

        tenant.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
