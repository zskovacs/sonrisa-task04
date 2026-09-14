using Microsoft.Extensions.Configuration;

namespace Sonrisa.Web.Ownership;

public sealed class ConfiguredCurrentOwner : ICurrentOwner
{
    public static readonly Guid DefaultOwnerId = Guid.Parse("d203a533-6bf8-4a21-98a9-291a74ef9f28");

    public ConfiguredCurrentOwner(IConfiguration configuration)
    {
        var configured = configuration["MvpOwner:Id"];
        if (configured is null)
        {
            OwnerId = DefaultOwnerId;
            return;
        }
        if (!Guid.TryParse(configured, out var parsed) || parsed == Guid.Empty)
            throw new InvalidOperationException("Invalid MvpOwner:Id configuration.");
        OwnerId = parsed;
    }

    public Guid OwnerId { get; }
}
