using System.Globalization;
using FluentValidation;

namespace Sonrisa.Web.Alerts;

public sealed class AlertInputValidator : AbstractValidator<AlertInput>
{
    public AlertInputValidator()
    {
        RuleFor(input => input.Name == null ? null : input.Name.Trim())
            .NotEmpty().WithMessage("Enter a name of 1 to 120 characters.")
            .MaximumLength(120).WithMessage("Enter a name of 1 to 120 characters.")
            .OverridePropertyName(nameof(AlertInput.Name));
        RuleFor(input => input.Threshold).Must(TryParseFinite)
            .WithMessage("Enter a finite number using a dot for decimals.");
    }

    public static bool TryParseFinite(string? text) =>
        double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
        && double.IsFinite(number);
}
