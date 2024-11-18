/// <summary>
/// Represents a plugin notification
/// </summary>
public class PluginNotification
{
    public string Title { get; }
    public string Message { get; }
    public NotificationType Type { get; }
    public DateTime Timestamp { get; }

    public PluginNotification(string title, string message, NotificationType type)
    {
        Title = title;
        Message = message;
        Type = type;
        Timestamp = DateTime.Now;
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
