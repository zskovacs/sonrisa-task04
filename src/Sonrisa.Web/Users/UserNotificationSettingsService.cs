using System.Data.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Data;
using Sonrisa.Web.Ownership;

namespace Sonrisa.Web.Users;

public sealed record UserNotificationSettingsDetails(string? EmailDestination, string? SlackDestination, Guid Revision);

public sealed class UserNotificationSettingsService(
    AppDbContext db, ICurrentOwner owner, IValidator<UserNotificationSettingsInput> validator,
    ILogger<UserNotificationSettingsService> logger)
{
    public async Task<AlertResult<UserNotificationSettingsDetails>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!HasConnection()) return AlertResult<UserNotificationSettingsDetails>.Unavailable();
        try
        {
            var profile = await db.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == owner.OwnerId, cancellationToken);
            return profile is null ? AlertResult<UserNotificationSettingsDetails>.NotFound()
                : AlertResult<UserNotificationSettingsDetails>.Success(ToDetails(profile));
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            LogUnavailable("get");
            return AlertResult<UserNotificationSettingsDetails>.Unavailable();
        }
    }

    public async Task<AlertResult<UserNotificationSettingsDetails>> SaveAsync(
        UserNotificationSettingsInput input, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
            return AlertResult<UserNotificationSettingsDetails>.Invalid(validation.Errors
                .Select(e => new AlertFieldError(e.PropertyName, e.ErrorMessage)).ToArray());
        if (!HasConnection()) return AlertResult<UserNotificationSettingsDetails>.Unavailable();
        try
        {
            var profile = await db.Users.SingleOrDefaultAsync(u => u.Id == owner.OwnerId, cancellationToken);
            if (profile is null)
            {
                if (input.Revision != Guid.Empty) return AlertResult<UserNotificationSettingsDetails>.Conflict();
                profile = new UserNotificationSettings { Id = owner.OwnerId, Revision = Guid.NewGuid() };
                db.Users.Add(profile);
            }
            else
            {
                if (input.Revision == Guid.Empty) return AlertResult<UserNotificationSettingsDetails>.Conflict();
                db.Entry(profile).Property(u => u.Revision).OriginalValue = input.Revision;
                profile.Revision = Guid.NewGuid();
            }
            profile.EmailDestination = UserNotificationSettingsInputValidator.Normalize(input.EmailDestination);
            profile.SlackDestination = UserNotificationSettingsInputValidator.Normalize(input.SlackDestination);
            await db.SaveChangesAsync(cancellationToken);
            return AlertResult<UserNotificationSettingsDetails>.Success(ToDetails(profile));
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return AlertResult<UserNotificationSettingsDetails>.Conflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return AlertResult<UserNotificationSettingsDetails>.Conflict();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            db.ChangeTracker.Clear();
            LogUnavailable("save");
            return AlertResult<UserNotificationSettingsDetails>.Unavailable();
        }
    }

    private static UserNotificationSettingsDetails ToDetails(UserNotificationSettings profile) =>
        new(profile.EmailDestination, profile.SlackDestination, profile.Revision);

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
        "Notification settings {Operation} unavailable due to database failure.", operation);
}
