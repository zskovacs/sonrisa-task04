namespace Sonrisa.Web.Runtime;

public sealed class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid AlertId { get; set; }
    public string Channel { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
}
