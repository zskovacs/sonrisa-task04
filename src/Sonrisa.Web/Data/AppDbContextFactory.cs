using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Sonrisa.Web.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        return CreateDbContext(configuration);
    }

    public static AppDbContext CreateDbContext(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("ProductDatabase");
        var options = new DbContextOptionsBuilder<AppDbContext>();
        // Provider metadata is sufficient for offline migration generation and scripting.
        // Applying a migration still requires an external configured connection.
        if (string.IsNullOrWhiteSpace(configured)) options.UseNpgsql();
        else options.UseNpgsql(configured);
        return new AppDbContext(options.Options);
    }
}
