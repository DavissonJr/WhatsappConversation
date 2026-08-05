using WhatsappCrmIA.Domain.Common;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Um lead/cliente que interage via WhatsApp.
/// </summary>
public class Contact : BaseEntity
{
    public string PhoneNumber { get; set; } = default!; // formato E.164
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public bool IsBlocked { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
