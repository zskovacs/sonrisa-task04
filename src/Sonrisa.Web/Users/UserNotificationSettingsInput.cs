namespace Sonrisa.Web.Users;

public sealed class UserNotificationSettingsInput
{
    public string? EmailDestination { get; set; }
    public string? SlackDestination { get; set; }
    public Guid Revision { get; set; }
}
