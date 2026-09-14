namespace Sonrisa.Web.Alerts;

public sealed class AlertInput
{
    public string? Name { get; set; }
    public string? Threshold { get; set; }
    public bool Enabled { get; set; }
    public Guid Revision { get; set; }
}
