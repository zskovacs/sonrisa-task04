namespace Sonrisa.Web.Runtime;

public sealed class SourceEvent
{
    public Guid Id { get; set; }
    public int ContractVersion { get; set; } = 1;
    public string Source { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string EventType { get; set; } = "earthquake";
    public DateTimeOffset OccurredAt { get; set; }
    public string Title { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string Data { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public string ProcessingStatus { get; set; } = "pending";
}
