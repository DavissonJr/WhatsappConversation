using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.AiAgent;

public record GetAiAgentConfigQuery : IRequest<AiAgentConfigDto?>;

public class GetAiAgentConfigHandler : IRequestHandler<GetAiAgentConfigQuery, AiAgentConfigDto?>
{
    private readonly IApplicationDbContext _db;
    public GetAiAgentConfigHandler(IApplicationDbContext db) => _db = db;

    public async Task<AiAgentConfigDto?> Handle(GetAiAgentConfigQuery request, CancellationToken ct)
    {
        var config = await _db.AiAgentConfigs.FirstOrDefaultAsync(ct);
        return config is null
            ? null
            : new AiAgentConfigDto(
                config.AgentName, config.SystemPrompt, config.AutoReplyEnabled,
                config.RequireHumanApproval, config.BusinessHours, config.FallbackMessage,
                !string.IsNullOrEmpty(config.AnthropicApiKeyEncrypted), config.AnthropicApiKeyPreview);
    }
}

public record UpdateAiAgentConfigCommand(
    string AgentName,
    string SystemPrompt,
    bool AutoReplyEnabled,
    bool RequireHumanApproval,
    string BusinessHours,
    string? FallbackMessage
) : IRequest<bool>;

public class UpdateAiAgentConfigHandler : IRequestHandler<UpdateAiAgentConfigCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public UpdateAiAgentConfigHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateAiAgentConfigCommand request, CancellationToken ct)
    {
        var config = await _db.AiAgentConfigs.FirstOrDefaultAsync(ct);
        if (config is null) return false;

        config.AgentName = request.AgentName;
        config.SystemPrompt = request.SystemPrompt;
        config.AutoReplyEnabled = request.AutoReplyEnabled;
        config.RequireHumanApproval = request.RequireHumanApproval;
        config.BusinessHours = request.BusinessHours;
        config.FallbackMessage = request.FallbackMessage;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>
/// Salva (ou troca) a chave da Anthropic do tenant. Só recebe a chave em
/// texto puro nesse exato momento, pra criptografar — depois disso ela
/// nunca mais é lida em texto puro pela API (só descriptografada em memória
/// na hora de chamar a Anthropic).
/// </summary>
public record SetAnthropicApiKeyCommand(string ApiKey) : IRequest<(bool Success, string? Error)>;

public class SetAnthropicApiKeyHandler : IRequestHandler<SetAnthropicApiKeyCommand, (bool Success, string? Error)>
{
    private readonly IApplicationDbContext _db;
    private readonly ISecretProtector _secretProtector;

    public SetAnthropicApiKeyHandler(IApplicationDbContext db, ISecretProtector secretProtector)
    {
        _db = db;
        _secretProtector = secretProtector;
    }

    public async Task<(bool Success, string? Error)> Handle(SetAnthropicApiKeyCommand request, CancellationToken ct)
    {
        var key = request.ApiKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length < 10)
            return (false, "Chave inválida.");

        var config = await _db.AiAgentConfigs.FirstOrDefaultAsync(ct);
        if (config is null) return (false, "Configuração do agente não encontrada.");

        config.AnthropicApiKeyEncrypted = _secretProtector.Encrypt(key);
        config.AnthropicApiKeyPreview = key.Length > 4 ? $"...{key[^4..]}" : "...";

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}

public record RemoveAnthropicApiKeyCommand : IRequest<bool>;

public class RemoveAnthropicApiKeyHandler : IRequestHandler<RemoveAnthropicApiKeyCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public RemoveAnthropicApiKeyHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(RemoveAnthropicApiKeyCommand request, CancellationToken ct)
    {
        var config = await _db.AiAgentConfigs.FirstOrDefaultAsync(ct);
        if (config is null) return false;

        config.AnthropicApiKeyEncrypted = null;
        config.AnthropicApiKeyPreview = null;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
