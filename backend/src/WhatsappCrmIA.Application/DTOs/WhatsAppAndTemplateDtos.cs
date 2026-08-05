namespace WhatsappCrmIA.Application.DTOs;

public record WhatsAppConnectionDto(
    Guid Id, string Label, string InstanceName, string? PhoneNumber, bool IsConnected);

public record MessageTemplateDto(
    Guid Id, string Name, string Scope, string Content, bool IsActive);
