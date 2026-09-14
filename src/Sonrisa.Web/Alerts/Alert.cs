namespace Sonrisa.Web.Alerts;

public sealed class Alert
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string EventType { get; set; } = "earthquake";
    public bool Enabled { get; set; }
    public Guid Revision { get; set; }
    public string ConditionField { get; set; } = "magnitude";
    public string ConditionOperator { get; set; } = "gte";
    public string ConditionValueType { get; set; } = "number";
    public double ConditionValue { get; set; }
    public Users.UserNotificationSettings Owner { get; set; } = null!;
}
