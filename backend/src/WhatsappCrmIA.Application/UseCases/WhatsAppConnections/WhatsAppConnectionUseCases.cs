using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Application.UseCases.WhatsAppConnections;

public record GetWhatsAppConnectionsQuery : IRequest<IReadOnlyList<WhatsAppConnectionDto>>;

public class GetWhatsAppConnectionsHandler
    : IRequestHandler<GetWhatsAppConnectionsQuery, IReadOnlyList<WhatsAppConnectionDto>>
{
    private readonly IApplicationDbContext _db;
    public GetWhatsAppConnectionsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<WhatsAppConnectionDto>> Handle(
        GetWhatsAppConnectionsQuery request, CancellationToken ct)
    {
        return await _db.WhatsAppConnections
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new WhatsAppConnectionDto(c.Id, c.Label, c.InstanceName, c.PhoneNumber, c.IsConnected))
            .ToListAsync(ct);
    }
}

public record CreateWhatsAppConnectionCommand(string Label) : IRequest<WhatsAppConnectionDto>;

public class CreateWhatsAppConnectionHandler
    : IRequestHandler<CreateWhatsAppConnectionCommand, WhatsAppConnectionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly ICurrentTenantService _currentTenant;

    public CreateWhatsAppConnectionHandler(
        IApplicationDbContext db, IWhatsAppGateway whatsApp, ICurrentTenantService currentTenant)
    {
        _db = db;
        _whatsApp = whatsApp;
        _currentTenant = currentTenant;
    }

    public async Task<WhatsAppConnectionDto> Handle(
        CreateWhatsAppConnectionCommand request, CancellationToken ct)
    {
        // Nome de instância único e legível: tenant + label + sufixo curto.
        var slug = request.Label.ToLowerInvariant().Replace(" ", "-");
        var instanceName = $"{_currentTenant.TenantId:N}-{slug}"[..Math.Min(40, $"{_currentTenant.TenantId:N}-{slug}".Length)];

        await _whatsApp.CreateInstanceAsync(instanceName, ct);

        var connection = new WhatsAppConnection
        {
            Label = request.Label,
            InstanceName = instanceName,
            IsConnected = false
        };
        _db.WhatsAppConnections.Add(connection);
        await _db.SaveChangesAsync(ct);

        return new WhatsAppConnectionDto(
            connection.Id, connection.Label, connection.InstanceName, connection.PhoneNumber, connection.IsConnected);
    }
}

public record GetQrCodeQuery(Guid ConnectionId) : IRequest<string?>;

public class GetQrCodeHandler : IRequestHandler<GetQrCodeQuery, string?>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;

    public GetQrCodeHandler(IApplicationDbContext db, IWhatsAppGateway whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    public async Task<string?> Handle(GetQrCodeQuery request, CancellationToken ct)
    {
        var connection = await _db.WhatsAppConnections.FirstOrDefaultAsync(c => c.Id == request.ConnectionId, ct);
        if (connection is null) return null;

        var qr = await _whatsApp.GetQrCodeAsync(connection.InstanceName, ct);
        connection.QrCodeBase64 = qr;
        await _db.SaveChangesAsync(ct);
        return qr;
    }
}

public record RefreshConnectionStatusCommand(Guid ConnectionId) : IRequest<bool>;

public class RefreshConnectionStatusHandler : IRequestHandler<RefreshConnectionStatusCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;

    public RefreshConnectionStatusHandler(IApplicationDbContext db, IWhatsAppGateway whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    public async Task<bool> Handle(RefreshConnectionStatusCommand request, CancellationToken ct)
    {
        var connection = await _db.WhatsAppConnections.FirstOrDefaultAsync(c => c.Id == request.ConnectionId, ct);
        if (connection is null) return false;

        var isConnected = await _whatsApp.IsConnectedAsync(connection.InstanceName, ct);
        connection.IsConnected = isConnected;
        if (isConnected && connection.ConnectedAtUtc is null)
            connection.ConnectedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return isConnected;
    }
}
