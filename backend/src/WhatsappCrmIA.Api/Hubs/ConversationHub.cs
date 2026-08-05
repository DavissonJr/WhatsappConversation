using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WhatsappCrmIA.Api.Hubs;

/// <summary>
/// Cada usuário conectado entra num "grupo" nomeado pelo TenantId, assim as
/// notificações de uma empresa nunca vazam para o painel de outra.
/// </summary>
[Authorize]
public class ConversationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        }

        await base.OnConnectedAsync();
    }
}
