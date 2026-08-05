using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Auth;

public record RegisterTenantCommand(
    string CompanyName,
    string Segment,
    string FullName,
    string Email,
    string Password
) : IRequest<AuthResult>;

public record AuthResult(bool Success, string? Token, string? ErrorMessage);

public class RegisterTenantHandler : IRequestHandler<RegisterTenantCommand, AuthResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterTenantHandler(
        IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResult> Handle(RegisterTenantCommand request, CancellationToken ct)
    {
        var emailInUse = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailInUse)
            return new AuthResult(false, null, "Este e-mail já está cadastrado.");

        var tenant = new Tenant
        {
            Name = request.CompanyName,
            Segment = request.Segment,
            Plan = PlanTier.Trial,
            IsActive = true
        };
        _db.Tenants.Add(tenant);

        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            Role = UserRole.Owner
        };
        _db.Users.Add(user);

        // Config padrão do agente de IA, já pronta para o tenant editar depois.
        _db.AiAgentConfigs.Add(new AiAgentConfig
        {
            TenantId = tenant.Id,
            AgentName = "Assistente Virtual",
            SystemPrompt = $"Você é o assistente virtual da empresa {request.CompanyName}, " +
                            $"do segmento {request.Segment}. Seja cordial, objetivo e sempre em pt-BR.",
            AutoReplyEnabled = true,
            RequireHumanApproval = true // começa conservador; o tenant ativa auto-envio quando confiar
        });

        await _db.SaveChangesAsync(ct);

        var token = _jwtTokenService.GenerateToken(user, tenant.Id);
        return new AuthResult(true, token, null);
    }
}
