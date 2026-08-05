namespace WhatsappCrmIA.Application.DTOs;

public record AiUsageLogDto(DateTime CreatedAtUtc, int InputTokens, int OutputTokens, decimal CostUsd);

public record AiUsageSummaryDto(
    int TotalInputTokens,
    int TotalOutputTokens,
    decimal EstimatedTotalCostUsd,
    IReadOnlyList<AiUsageLogDto> RecentUsage);
