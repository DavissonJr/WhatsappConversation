namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Guarda um cadastro que ainda não foi confirmado por e-mail. O Tenant/User
/// de verdade só são criados depois que o código for validado — assim, um
/// bot que só preenche o formulário sem ter acesso ao e-mail nunca vira uma
/// conta de verdade no sistema.
/// </summary>
public class PendingRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CompanyName { get; set; } = default!;
    public string Segment { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    public string VerificationCode { get; set; } = default!;
    public int AttemptCount { get; set; } = 0;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
