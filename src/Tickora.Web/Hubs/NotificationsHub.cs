using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Tickora.Application;

namespace Tickora.Web.Hubs;

[Authorize]
public sealed class NotificationsHub(Tickora.Web.Security.CurrentUser user) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await user.ResolveAsync(Context.User!, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
    // No client-controlled groups. Clients can receive only their authenticated user stream.
}
public sealed class SignalRNotificationPublisher(IHubContext<NotificationsHub> hub, NotificationPulse pulse, ILogger<SignalRNotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAsync(string recipient, CancellationToken ct = default)
    {
        pulse.Publish(recipient);
        try { await hub.Clients.User(recipient).SendAsync("notificationsChanged", cancellationToken: ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogWarning(ex, "Notification persisted; realtime hint could not be delivered"); }
    }
}
