using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.AiUsage;

public record GetAiUsageQuery : IRequest<AiUsageSummaryDto?>;

public class GetAiUsageHandler : IRequestHandler<GetAiUsageQuery, AiUsageSummaryDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public GetAiUsageHandler(IApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<AiUsageSummaryDto?> Handle(GetAiUsageQuery request, CancellationToken ct)
    {
        if (_currentTenant.TenantId is null) return null;

        var totalInputTokens = await _db.AiUsageLogs.SumAsync(u => (int?)u.InputTokens, ct) ?? 0;
        var totalOutputTokens = await _db.AiUsageLogs.SumAsync(u => (int?)u.OutputTokens, ct) ?? 0;
        var totalSpent = await _db.AiUsageLogs.SumAsync(u => (decimal?)u.CostUsd, ct) ?? 0m;

        var recent = await _db.AiUsageLogs
            .OrderByDescending(u => u.CreatedAtUtc)
            .Take(30)
            .Select(u => new AiUsageLogDto(u.CreatedAtUtc, u.InputTokens, u.OutputTokens, u.CostUsd))
            .ToListAsync(ct);

        return new AiUsageSummaryDto(totalInputTokens, totalOutputTokens, totalSpent, recent);
    }
}
