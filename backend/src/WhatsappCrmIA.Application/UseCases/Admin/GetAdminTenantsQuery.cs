using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Admin;

/// <summary>
/// Lista TODAS as empresas cadastradas no SaaS, com métricas agregadas de
/// cada uma. Ignora os filtros de tenant de propósito — é o único lugar do
/// sistema que enxerga tudo de todo mundo, por isso é protegido pela policy
/// "PlatformAdmin" no controller (não é uma rota comum autenticada).
/// </summary>
public record GetAdminTenantsQuery : IRequest<IReadOnlyList<AdminTenantSummaryDto>>;

public class GetAdminTenantsHandler : IRequestHandler<GetAdminTenantsQuery, IReadOnlyList<AdminTenantSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAdminTenantsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminTenantSummaryDto>> Handle(GetAdminTenantsQuery request, CancellationToken ct)
    {
        var tenants = await _db.Tenants.OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct);

        var owners = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Owner)
            .ToListAsync(ct);
        var ownerByTenant = owners
            .GroupBy(u => u.TenantId)
            .ToDictionary(g => g.Key, g => g.OrderBy(u => u.CreatedAtUtc).First());

        var userCounts = await _db.Users.IgnoreQueryFilters()
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var connections = await _db.WhatsAppConnections.IgnoreQueryFilters()
            .GroupBy(w => w.TenantId)
            .Select(g => new { TenantId = g.Key, Total = g.Count(), Connected = g.Count(w => w.IsConnected) })
            .ToListAsync(ct);
        var connectionsByTenant = connections.ToDictionary(x => x.TenantId, x => x);

        var contactCounts = await _db.Contacts.IgnoreQueryFilters()
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var conversationCounts = await _db.Conversations.IgnoreQueryFilters()
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var messageCounts = await _db.Messages.IgnoreQueryFilters()
            .GroupBy(m => m.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count(), LastAt = g.Max(m => m.CreatedAtUtc) })
            .ToListAsync(ct);
        var messagesByTenant = messageCounts.ToDictionary(x => x.TenantId, x => x);

        var appointmentCounts = await _db.Appointments.IgnoreQueryFilters()
            .GroupBy(a => a.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var proposalCounts = await _db.Proposals.IgnoreQueryFilters()
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var aiUsage = await _db.AiUsageLogs.IgnoreQueryFilters()
            .GroupBy(u => u.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                InputTokens = g.Sum(u => (long)u.InputTokens),
                OutputTokens = g.Sum(u => (long)u.OutputTokens),
                CostUsd = g.Sum(u => u.CostUsd)
            })
            .ToListAsync(ct);
        var aiUsageByTenant = aiUsage.ToDictionary(x => x.TenantId, x => x);

        return tenants.Select(t =>
        {
            ownerByTenant.TryGetValue(t.Id, out var owner);
            connectionsByTenant.TryGetValue(t.Id, out var conn);
            messagesByTenant.TryGetValue(t.Id, out var msg);
            aiUsageByTenant.TryGetValue(t.Id, out var ai);

            return new AdminTenantSummaryDto(
                t.Id, t.Name, t.Segment, t.Plan.ToString(), t.IsActive, t.CreatedAtUtc,
                owner?.FullName, owner?.Email,
                userCounts.GetValueOrDefault(t.Id, 0),
                conn?.Total ?? 0, conn?.Connected ?? 0,
                contactCounts.GetValueOrDefault(t.Id, 0),
                conversationCounts.GetValueOrDefault(t.Id, 0),
                msg?.Count ?? 0,
                appointmentCounts.GetValueOrDefault(t.Id, 0),
                proposalCounts.GetValueOrDefault(t.Id, 0),
                ai?.InputTokens ?? 0, ai?.OutputTokens ?? 0, ai?.CostUsd ?? 0m,
                msg?.LastAt);
        }).ToList();
    }
}
