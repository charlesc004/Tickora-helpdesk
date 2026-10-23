namespace Tickora.Web.Hubs;

// Interactive Server circuits share this lightweight hint dispatcher; no second browser hub is needed.
public sealed class NotificationPulse
{
    public event Action<string>? Changed;
    public void Publish(string recipient) => Changed?.Invoke(recipient);
}
