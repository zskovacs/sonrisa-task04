namespace Sonrisa.Web.Alerts;

public enum AlertStatus { Success, NotFound, Conflict, Invalid, Unavailable }

public sealed record AlertFieldError(string Key, string Message);

public sealed record AlertDetails(Guid Id, string Name, double Threshold, bool Enabled, Guid Revision);

public sealed record AlertResult<T>(AlertStatus Status, T? Value = default,
    IReadOnlyList<AlertFieldError>? Errors = null)
{
    public static AlertResult<T> Success(T value) => new(AlertStatus.Success, value);
    public static AlertResult<T> NotFound() => new(AlertStatus.NotFound);
    public static AlertResult<T> Conflict() => new(AlertStatus.Conflict);
    public static AlertResult<T> Invalid(IReadOnlyList<AlertFieldError> errors) => new(AlertStatus.Invalid, default, errors);
    public static AlertResult<T> Unavailable() => new(AlertStatus.Unavailable);
}
