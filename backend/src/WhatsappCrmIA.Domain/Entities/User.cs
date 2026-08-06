using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Usuário que acessa o painel (dono da empresa ou atendente).
/// Não filtramos por query global de tenant aqui, pois o login precisa
/// localizar o usuário pelo e-mail antes de saber o tenant.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public UserRole Role { get; set; } = UserRole.Owner;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Marca quem administra o SaaS em si (você) — dá acesso ao painel
    /// administrativo que enxerga TODAS as empresas cadastradas, não só a
    /// própria. Não existe forma de ativar isso pela interface por segurança;
    /// só é ligado direto no banco (ver README).
    /// </summary>
    public bool IsPlatformAdmin { get; set; } = false;
}
