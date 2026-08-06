using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Auth;

public record ResendVerificationCodeCommand(string Email) : IRequest<(bool Success, string? Error)>;

public class ResendVerificationCodeHandler : IRequestHandler<ResendVerificationCodeCommand, (bool Success, string? Error)>
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _emailSender;

    public ResendVerificationCodeHandler(IApplicationDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task<(bool Success, string? Error)> Handle(ResendVerificationCodeCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);

        if (pending is null)
            return (false, "Não encontramos um cadastro pendente para esse e-mail.");

        pending.VerificationCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        pending.ExpiresAtUtc = DateTime.UtcNow.Add(CodeLifetime);
        pending.AttemptCount = 0;
        await _db.SaveChangesAsync(ct);

        var html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;">
              <h2 style="color: #0b0f14;">Olá, {pending.FullName}!</h2>
              <p>Seu novo código de confirmação do Zappy CRM:</p>
              <div style="font-size: 32px; font-weight: 800; letter-spacing: 8px; background: #f7f9fb; padding: 16px 24px; border-radius: 12px; text-align: center; margin: 24px 0;">
                {pending.VerificationCode}
              </div>
              <p style="color: #64748b; font-size: 13px;">Esse código expira em 15 minutos.</p>
            </div>
            """;
        await _emailSender.SendAsync(email, "Novo código de confirmação — Zappy CRM", html, ct);

        return (true, null);
    }
}
