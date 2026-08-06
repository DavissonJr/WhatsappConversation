using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Application.UseCases.Auth;

/// <summary>
/// Passo 1 do cadastro: valida os dados, gera um código de 6 dígitos e manda
/// por e-mail. A conta de verdade (Tenant/User) só é criada depois que o
/// código for confirmado (ver VerifyRegistrationCommand) — isso dificulta
/// bots criarem contas em massa, já que precisam de acesso real ao e-mail.
/// </summary>
public record RegisterTenantCommand(
    string CompanyName,
    string Segment,
    string FullName,
    string Email,
    string Password
) : IRequest<AuthResult>;

public record AuthResult(bool Success, string? Token, string? ErrorMessage, bool RequiresVerification = false);

public class RegisterTenantHandler : IRequestHandler<RegisterTenantCommand, AuthResult>
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;

    public RegisterTenantHandler(
        IApplicationDbContext db, IPasswordHasher passwordHasher, IEmailSender emailSender)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
    }

    public async Task<AuthResult> Handle(RegisterTenantCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailInUse = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailInUse)
            return new AuthResult(false, null, "Este e-mail já está cadastrado.");

        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

        // Se já tinha um cadastro pendente com esse e-mail (ex: tentou antes e
        // não confirmou), substitui pelo novo — evita erro de duplicidade e
        // deixa a pessoa tentar de novo à vontade.
        var existing = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (existing is not null) _db.PendingRegistrations.Remove(existing);

        _db.PendingRegistrations.Add(new PendingRegistration
        {
            CompanyName = request.CompanyName,
            Segment = request.Segment,
            FullName = request.FullName,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            VerificationCode = code,
            ExpiresAtUtc = DateTime.UtcNow.Add(CodeLifetime)
        });

        await _db.SaveChangesAsync(ct);

        await _emailSender.SendAsync(email, "Seu código de confirmação — Zappy CRM", BuildEmailHtml(request.FullName, code), ct);

        return new AuthResult(false, null, null, RequiresVerification: true);
    }

    private static string BuildEmailHtml(string fullName, string code) => $"""
        <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;">
          <h2 style="color: #0b0f14;">Olá, {fullName}!</h2>
          <p>Use o código abaixo para confirmar seu cadastro no Zappy CRM:</p>
          <div style="font-size: 32px; font-weight: 800; letter-spacing: 8px; background: #f7f9fb; padding: 16px 24px; border-radius: 12px; text-align: center; margin: 24px 0;">
            {code}
          </div>
          <p style="color: #64748b; font-size: 13px;">Esse código expira em 15 minutos. Se você não pediu esse cadastro, pode ignorar este e-mail.</p>
        </div>
        """;
}
