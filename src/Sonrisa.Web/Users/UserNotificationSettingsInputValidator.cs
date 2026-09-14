using System.Net.Mail;
using FluentValidation;

namespace Sonrisa.Web.Users;

public sealed class UserNotificationSettingsInputValidator : AbstractValidator<UserNotificationSettingsInput>
{
    public UserNotificationSettingsInputValidator()
    {
        RuleFor(input => input).Must(input =>
                Normalize(input.EmailDestination) is not null || Normalize(input.SlackDestination) is not null)
            .WithMessage("Enter an email or Slack destination.");
        RuleFor(input => input.EmailDestination)
            .Must(value => !ContainsControl(value)).WithMessage("Email must not contain control characters.");
        RuleFor(input => Normalize(input.EmailDestination))
            .MaximumLength(254).WithMessage("Email destination must be at most 254 characters.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .Must(IsBareMailbox).WithMessage("Enter a single email mailbox.")
            .OverridePropertyName(nameof(UserNotificationSettingsInput.EmailDestination))
            .When(input => Normalize(input.EmailDestination) is not null);
        RuleFor(input => input.SlackDestination)
            .Must(value => !ContainsControl(value)).WithMessage("Slack ID must not contain control characters.");
        RuleFor(input => Normalize(input.SlackDestination))
            .Length(2, 80).WithMessage("Slack ID must be 2 to 80 characters.")
            .Matches(@"\A[A-Z0-9]+\z").WithMessage("Enter an uppercase Slack channel ID.")
            .OverridePropertyName(nameof(UserNotificationSettingsInput.SlackDestination))
            .When(input => Normalize(input.SlackDestination) is not null);
    }

    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool IsBareMailbox(string? value)
    {
        var email = Normalize(value);
        return email is not null && MailAddress.TryCreate(email, out var parsed)
               && parsed.Address == email && parsed.DisplayName == ""
               && !email.Contains("..", StringComparison.Ordinal)
               && !email.StartsWith(".", StringComparison.Ordinal);
    }

    private static bool ContainsControl(string? value) => value?.Any(char.IsControl) == true;
}
