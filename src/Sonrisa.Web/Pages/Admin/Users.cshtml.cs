using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sonrisa.Web.Data;

namespace Sonrisa.Web.Pages.Admin;

public sealed class UsersModel(AppDbContext db, ILogger<UsersModel> logger) : AdminPageModel(db, logger)
{
    public IReadOnlyList<AdminUserRow> Owners { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!HasConnection()) return Page();
        try
        {
            Owners = await Db.Users.AsNoTracking()
                .OrderBy(user => user.Id)
                .Select(user => new AdminUserRow(
                    user.Id, user.Alerts.Count, user.Alerts.Count(alert => alert.Enabled),
                    user.SlackDestination != null, user.EmailDestination != null))
                .ToListAsync(cancellationToken);
            return Page();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            return UnavailablePage("owners");
        }
    }
}

public sealed record AdminUserRow(Guid Id, int AlertCount, int EnabledCount, bool HasSlack, bool HasEmail);
