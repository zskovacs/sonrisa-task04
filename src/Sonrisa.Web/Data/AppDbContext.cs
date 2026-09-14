using Microsoft.EntityFrameworkCore;

namespace Sonrisa.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
}
