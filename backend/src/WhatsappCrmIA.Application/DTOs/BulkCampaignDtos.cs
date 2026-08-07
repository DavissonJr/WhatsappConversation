namespace WhatsappCrmIA.Application.DTOs;

/// <summary>
/// Parâmetros de segmentação de público — vai crescendo conforme surgir
/// necessidade (esses cobrem os pedidos iniciais: inatividade e "nunca
/// agendou / não agenda há X dias").
/// </summary>
public record BulkAudienceFilters(
    bool ExcludeBlocked,
    int? NoAppointmentInLastDays,
    int? NoConversationInLastDays,
    string? SearchTerm);

public record BulkCampaignSummaryDto(
    Guid Id,
    string Title,
    string MessageText,
    string WhatsAppConnectionLabel,
    string Status,
    int DelaySeconds,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
