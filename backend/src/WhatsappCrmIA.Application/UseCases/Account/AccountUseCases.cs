using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Account;

public record GetMeQuery : IRequest<MeDto?>;

public class GetMeHandler : IRequestHandler<GetMeQuery, MeDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MeDto?> Handle(GetMeQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : new MeDto(user.Id, user.FullName, user.Email, user.Role.ToString());
    }
}

public record UpdateProfileCommand(string FullName, string Email) : IRequest<(bool Success, string? Error)>;

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, (bool Success, string? Error)>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<(bool Success, string? Error)> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return (false, "Usuário não identificado.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (false, "Usuário não encontrado.");

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId, ct);
        if (emailTaken) return (false, "Esse e-mail já está em uso por outra conta.");

        user.FullName = request.FullName;
        user.Email = request.Email;

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<(bool Success, string? Error)>;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, (bool Success, string? Error)>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<(bool Success, string? Error)> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return (false, "Usuário não identificado.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (false, "Usuário não encontrado.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return (false, "Senha atual incorreta.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}
