using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string GenerateToken(User user, Guid tenantId);
}
