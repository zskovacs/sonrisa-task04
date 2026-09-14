using Sonrisa.Web.Alerts;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class AlertInputValidatorTests
{
    private readonly AlertInputValidator alertValidator = new();
    private readonly UserNotificationSettingsInputValidator settingsValidator = new();

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e500")]
    [InlineData("1,000")]
    [InlineData("")]
    public void Rejects_nonfinite_or_ambiguous_threshold(string threshold)
    {
        Assert.Contains(alertValidator.Validate(new AlertInput { Name = "Quake", Threshold = threshold }).Errors,
            e => e.PropertyName == nameof(AlertInput.Threshold));
    }

    [Theory]
    [InlineData("-2.5")]
    [InlineData("0")]
    [InlineData("5.5")]
    public void Accepts_finite_invariant_thresholds(string threshold)
    {
        Assert.True(alertValidator.Validate(new AlertInput { Name = "Quake", Threshold = threshold }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_blank_name(string name)
    {
        Assert.Contains(alertValidator.Validate(new AlertInput { Name = name, Threshold = "5" }).Errors,
            e => e.PropertyName == nameof(AlertInput.Name));
    }

    [Fact]
    public void Rejects_oversized_name()
    {
        Assert.False(alertValidator.Validate(new AlertInput { Name = new string('x', 121), Threshold = "5" }).IsValid);
    }

    [Theory]
    [InlineData("Name <person@example.test>")]
    [InlineData("person@example.test\r\nBcc:x@example.test")]
    [InlineData("person@example.test\n")]
    [InlineData("not-mailbox")]
    [InlineData("person@example..test")]
    [InlineData(".person@example.test")]
    public void Rejects_invalid_email_form(string email)
    {
        Assert.Contains(settingsValidator.Validate(new UserNotificationSettingsInput { EmailDestination = email }).Errors,
            e => e.PropertyName == nameof(UserNotificationSettingsInput.EmailDestination));
    }

    [Fact]
    public void Accepts_mailbox_without_allowlist()
    {
        Assert.True(settingsValidator.Validate(new UserNotificationSettingsInput
        { EmailDestination = " other@example.test " }).IsValid);
    }

    [Fact]
    public void Rejects_oversized_email_and_slack_destinations()
    {
        Assert.False(settingsValidator.Validate(new UserNotificationSettingsInput
        { EmailDestination = new string('a', 245) + "@example.test" }).IsValid);
        Assert.False(settingsValidator.Validate(new UserNotificationSettingsInput
        { SlackDestination = new string('C', 81) }).IsValid);
    }

    [Theory]
    [InlineData("C12345678\n")]
    [InlineData("#general")]
    [InlineData("https://slack.example")]
    [InlineData("c12345678")]
    public void Rejects_invalid_slack_id(string slack)
    {
        Assert.Contains(settingsValidator.Validate(new UserNotificationSettingsInput { SlackDestination = slack }).Errors,
            e => e.PropertyName == nameof(UserNotificationSettingsInput.SlackDestination));
    }

    [Fact]
    public void Requires_one_destination()
    {
        Assert.False(settingsValidator.Validate(new UserNotificationSettingsInput()).IsValid);
    }
}
