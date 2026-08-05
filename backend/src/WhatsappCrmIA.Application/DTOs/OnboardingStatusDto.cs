namespace WhatsappCrmIA.Application.DTOs;

public record OnboardingStatusDto(
    bool HasConnectedWhatsApp,
    bool HasAnthropicApiKey,
    bool HasSentOrReceivedMessage);
