using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sonrisa.Web.Data;

namespace Sonrisa.Web.Pages.Admin;

public abstract class AdminPageModel(AppDbContext db, ILogger logger) : PageModel
{
    protected AppDbContext Db { get; } = db;

    public bool Unavailable { get; private set; }

    protected bool HasConnection()
    {
        var configured = Db.Database.GetConnectionString();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var parsed = new NpgsqlConnectionStringBuilder(configured);
                if (!string.IsNullOrWhiteSpace(parsed.Host)) return true;
            }
            catch (ArgumentException) { }
        }
        SetUnavailable("configuration");
        return false;
    }

    protected IActionResult UnavailablePage(string operation)
    {
        SetUnavailable(operation);
        return Page();
    }

    protected static bool IsDatabaseFailure(Exception exception) =>
        exception is DbException or DbUpdateException or TimeoutException
            or InvalidOperationException { InnerException: DbException or TimeoutException };

    private void SetUnavailable(string operation)
    {
        Unavailable = true;
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        logger.LogWarning("Admin configuration {Operation} unavailable due to database failure.", operation);
    }
}
