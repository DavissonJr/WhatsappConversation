namespace WhatsappCrmIA.Application.DTOs;

public record AiUsageLogDto(DateTime CreatedAtUtc, int InputTokens, int OutputTokens, decimal CostUsd);

public record AiUsageSummaryDto(
    decimal BalanceUsd,
    decimal TotalSpentUsd,
    IReadOnlyList<AiUsageLogDto> RecentUsage);
