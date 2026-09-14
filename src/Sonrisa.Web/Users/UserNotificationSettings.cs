using Sonrisa.Web.Alerts;

namespace Sonrisa.Web.Users;

public sealed class UserNotificationSettings
{
    public Guid Id { get; set; }
    public string? EmailDestination { get; set; }
    public string? SlackDestination { get; set; }
    public Guid Revision { get; set; }
    public List<Alert> Alerts { get; set; } = [];
}
