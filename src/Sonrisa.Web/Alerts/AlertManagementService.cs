using System.Data.Common;
using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;

namespace Sonrisa.Web.Alerts;

public sealed class AlertManagementService(
    AppDbContext db, ICurrentOwner owner, IValidator<AlertInput> validator,
    ILogger<AlertManagementService> logger)
{
    public async Task<AlertResult<IReadOnlyList<AlertDetails>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!HasConnection()) return AlertResult<IReadOnlyList<AlertDetails>>.Unavailable();
        try
        {
            var alerts = await db.Alerts.AsNoTracking().Where(a => a.OwnerId == owner.OwnerId)
                .OrderBy(a => a.Name).ThenBy(a => a.Id).ToListAsync(cancellationToken);
            return AlertResult<IReadOnlyList<AlertDetails>>.Success(alerts.Select(ToDetails).ToArray());
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            LogUnavailable("list");
            return AlertResult<IReadOnlyList<AlertDetails>>.Unavailable();
        }
    }

    public async Task<AlertResult<AlertDetails>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!HasConnection()) return AlertResult<AlertDetails>.Unavailable();
        try
        {
            var alert = await db.Alerts.AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == id && a.OwnerId == owner.OwnerId, cancellationToken);
            return alert is null ? AlertResult<AlertDetails>.NotFound() : AlertResult<AlertDetails>.Success(ToDetails(alert));
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            LogUnavailable("get");
            return AlertResult<AlertDetails>.Unavailable();
        }
    }

    public async Task<AlertResult<AlertDetails>> CreateAsync(AlertInput input, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid) return AlertResult<AlertDetails>.Invalid(ToErrors(validation));
        if (!HasConnection()) return AlertResult<AlertDetails>.Unavailable();
        try
        {
            if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == owner.OwnerId, cancellationToken))
                return AlertResult<AlertDetails>.Invalid([new("Settings", "Save notification settings before creating an alert.")]);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            LogUnavailable("create");
            return AlertResult<AlertDetails>.Unavailable();
        }
        var alert = new Alert
        {
            Id = Guid.NewGuid(), OwnerId = owner.OwnerId, Name = input.Name!.Trim(),
            ConditionValue = ParseThreshold(input.Threshold), Enabled = input.Enabled, Revision = Guid.NewGuid()
        };
        db.Alerts.Add(alert);
        return await SaveAsync(alert, "create", cancellationToken);
    }

    public async Task<AlertResult<AlertDetails>> UpdateAsync(Guid id, AlertInput input,
        CancellationToken cancellationToken = default)
    {
        if (!HasConnection()) return AlertResult<AlertDetails>.Unavailable();
        Alert? alert;
        try { alert = await LoadOwnedAsync(id, cancellationToken); }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            LogUnavailable("update");
            return AlertResult<AlertDetails>.Unavailable();
        }
        if (alert is null) return AlertResult<AlertDetails>.NotFound();
        if (input.Revision == Guid.Empty)
            return AlertResult<AlertDetails>.Invalid([new("Revision", "Reload this alert before saving.")]);
        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid) return AlertResult<AlertDetails>.Invalid(ToErrors(validation));
        db.Entry(alert).Property(a => a.Revision).OriginalValue = input.Revision;
        alert.Revision = Guid.NewGuid();
        alert.Name = input.Name!.Trim();
        alert.ConditionValue = ParseThreshold(input.Threshold);
        alert.Enabled = input.Enabled;
        return await SaveAsync(alert, "update", cancellationToken);
    }

    public async Task<AlertResult<AlertDetails>> SetEnabledAsync(Guid id, Guid revision, bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (!HasConnection()) return AlertResult<AlertDetails>.Unavailable();
        Alert? alert;
        try { alert = await LoadOwnedAsync(id, cancellationToken); }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            LogUnavailable("status");
            return AlertResult<AlertDetails>.Unavailable();
        }
        if (alert is null) return AlertResult<AlertDetails>.NotFound();
        if (revision == Guid.Empty)
            return AlertResult<AlertDetails>.Invalid([new("Revision", "Reload this alert before changing status.")]);
        if (enabled)
        {
            var retained = new AlertInput { Name = alert.Name,
                Threshold = alert.ConditionValue.ToString("R", CultureInfo.InvariantCulture), Enabled = true };
            var validation = await validator.ValidateAsync(retained, cancellationToken);
            if (!validation.IsValid) return AlertResult<AlertDetails>.Invalid(ToErrors(validation));
        }
        db.Entry(alert).Property(a => a.Revision).OriginalValue = revision;
        alert.Revision = Guid.NewGuid();
        alert.Enabled = enabled;
        return await SaveAsync(alert, "status", cancellationToken);
    }

    private Task<Alert?> LoadOwnedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Alerts.SingleOrDefaultAsync(a => a.Id == id && a.OwnerId == owner.OwnerId, cancellationToken);

    private async Task<AlertResult<AlertDetails>> SaveAsync(Alert alert, string operation, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return AlertResult<AlertDetails>.Success(ToDetails(alert));
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return AlertResult<AlertDetails>.Conflict();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            db.ChangeTracker.Clear();
            LogUnavailable(operation);
            return AlertResult<AlertDetails>.Unavailable();
        }
    }

    private static double ParseThreshold(string? text) =>
        double.Parse(text!.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    private static AlertDetails ToDetails(Alert alert) => new(
        alert.Id, alert.Name, alert.ConditionValue, alert.Enabled, alert.Revision);
    private static IReadOnlyList<AlertFieldError> ToErrors(FluentValidation.Results.ValidationResult result) =>
        result.Errors.Select(e => new AlertFieldError(e.PropertyName, e.ErrorMessage)).ToArray();

    private bool HasConnection()
    {
        var configured = db.Database.GetConnectionString();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try { _ = new NpgsqlConnectionStringBuilder(configured); return true; }
            catch (ArgumentException) { }
        }
        LogUnavailable("configuration");
        return false;
    }

    private static bool IsDatabaseFailure(Exception exception) => exception is DbException or DbUpdateException or TimeoutException;
    private void LogUnavailable(string operation) => logger.LogWarning(
        "Alert configuration {Operation} unavailable due to database failure.", operation);
}
