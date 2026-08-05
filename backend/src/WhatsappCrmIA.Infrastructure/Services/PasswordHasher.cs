using Microsoft.AspNetCore.Identity;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Usa o PasswordHasher do ASP.NET Identity (PBKDF2 + salt), sem precisar
/// do sistema de Identity inteiro — só o algoritmo de hash mesmo.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string password, string hash) =>
        _inner.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
