using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginHandler(
        IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken ct)
    {
        // Users não tem query filter de tenant (não temos tenant atual antes do login).
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return new AuthResult(false, null, "E-mail ou senha inválidos.");

        var token = _jwtTokenService.GenerateToken(user, user.TenantId);
        return new AuthResult(true, token, null);
    }
}
