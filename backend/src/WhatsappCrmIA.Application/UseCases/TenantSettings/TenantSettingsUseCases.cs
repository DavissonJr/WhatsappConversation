using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.TenantSettings;

public record GetTenantSettingsQuery : IRequest<TenantSettingsDto?>;

public class GetTenantSettingsHandler : IRequestHandler<GetTenantSettingsQuery, TenantSettingsDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public GetTenantSettingsHandler(IApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<TenantSettingsDto?> Handle(GetTenantSettingsQuery request, CancellationToken ct)
    {
        if (_currentTenant.TenantId is not { } tenantId) return null;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        return tenant is null
            ? null
            : new TenantSettingsDto(tenant.Id, tenant.Name, tenant.Segment, tenant.Plan.ToString());
    }
}

public record UpdateTenantSettingsCommand(string Name, string Segment) : IRequest<bool>;

public class UpdateTenantSettingsHandler : IRequestHandler<UpdateTenantSettingsCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public UpdateTenantSettingsHandler(IApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<bool> Handle(UpdateTenantSettingsCommand request, CancellationToken ct)
    {
        if (_currentTenant.TenantId is not { } tenantId) return false;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return false;

        tenant.Name = request.Name;
        tenant.Segment = request.Segment;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
