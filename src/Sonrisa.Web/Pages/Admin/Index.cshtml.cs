using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sonrisa.Web.Data;

namespace Sonrisa.Web.Pages.Admin;

public sealed class IndexModel(AppDbContext db, ILogger<IndexModel> logger) : AdminPageModel(db, logger)
{
    public int Owners { get; private set; }
    public int Alerts { get; private set; }
    public int EnabledAlerts { get; private set; }
    public int DisabledAlerts => Alerts - EnabledAlerts;
    public int OwnersWithSlack { get; private set; }
    public int OwnersWithEmail { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!HasConnection()) return Page();
        try
        {
            var owners = await Db.Users.AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Slack = group.Count(user => user.SlackDestination != null),
                    Email = group.Count(user => user.EmailDestination != null)
                })
                .SingleOrDefaultAsync(cancellationToken);
            Owners = owners?.Total ?? 0;
            OwnersWithSlack = owners?.Slack ?? 0;
            OwnersWithEmail = owners?.Email ?? 0;

            var alerts = await Db.Alerts.AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new { Total = group.Count(), Enabled = group.Count(alert => alert.Enabled) })
                .SingleOrDefaultAsync(cancellationToken);
            Alerts = alerts?.Total ?? 0;
            EnabledAlerts = alerts?.Enabled ?? 0;
            return Page();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            return UnavailablePage("overview");
        }
    }
}
