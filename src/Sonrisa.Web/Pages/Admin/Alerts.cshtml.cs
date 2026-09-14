using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sonrisa.Web.Data;

namespace Sonrisa.Web.Pages.Admin;

public sealed class AlertsModel(AppDbContext db, ILogger<AlertsModel> logger) : AdminPageModel(db, logger)
{
    public IReadOnlyList<AdminAlertRow> Alerts { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!HasConnection()) return Page();
        try
        {
            Alerts = await Db.Alerts.AsNoTracking()
                .OrderBy(alert => alert.OwnerId).ThenBy(alert => alert.Name).ThenBy(alert => alert.Id)
                .Select(alert => new AdminAlertRow(
                    alert.OwnerId, alert.Name, alert.EventType, alert.Enabled,
                    alert.ConditionField, alert.ConditionOperator, alert.ConditionValueType, alert.ConditionValue,
                    alert.Owner.SlackDestination != null, alert.Owner.EmailDestination != null))
                .ToListAsync(cancellationToken);
            return Page();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            return UnavailablePage("alerts");
        }
    }
}

public sealed record AdminAlertRow(
    Guid OwnerId, string Name, string EventType, bool Enabled,
    string ConditionField, string ConditionOperator, string ConditionValueType, double ConditionValue,
    bool HasSlack, bool HasEmail)
{
    public string Condition => $"{ConditionField} {(ConditionOperator == "gte" ? ">=" : ConditionOperator)} {ConditionValue.ToString("R", CultureInfo.InvariantCulture)} ({ConditionValueType})";
    public string Channels => (HasSlack, HasEmail) switch
    {
        (true, true) => "Slack + Email",
        (true, false) => "Slack",
        (false, true) => "Email",
        _ => "None"
    };
}
