using System.Text.Json;
using Sonrisa.Web.Users;
using Xunit;

namespace Sonrisa.Web.Tests;

public sealed class DestinationContractTests
{
    private static readonly DestinationVector[] Vectors = JsonSerializer.Deserialize<DestinationVector[]>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "destination-contract.json")),
        new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    [Fact]
    public void Validator_matches_shared_destination_contract()
    {
        var validator = new UserNotificationSettingsInputValidator();

        foreach (var vector in Vectors)
        {
            var result = validator.Validate(new UserNotificationSettingsInput
            {
                EmailDestination = vector.Email,
                SlackDestination = vector.Slack
            });

            Assert.True(result.IsValid == vector.IsValid,
                $"{vector.Name}: expected valid={vector.IsValid}; errors={string.Join(" | ", result.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"))}");
        }
    }

    [Fact]
    public void Normalization_matches_shared_destination_contract()
    {
        foreach (var vector in Vectors)
        {
            Assert.Equal(vector.NormalizedEmail,
                UserNotificationSettingsInputValidator.Normalize(vector.Email));
            Assert.Equal(vector.NormalizedSlack,
                UserNotificationSettingsInputValidator.Normalize(vector.Slack));
        }
    }

    private sealed record DestinationVector(
        string Name,
        string? Email,
        string? Slack,
        bool IsValid,
        string? NormalizedEmail,
        string? NormalizedSlack,
        string[] RuntimeChannels,
        string[] RuntimeDiagnostics);
}
