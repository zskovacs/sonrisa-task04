using Microsoft.EntityFrameworkCore;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Runtime;
using Sonrisa.Web.Users;

namespace Sonrisa.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    // Match the Unicode whitespace set used by .NET string.Trim, without database-locale dependence.
    private const string TrimCharacters = """U&'\0009\000A\000B\000C\000D\0020\0085\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000'""";

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<UserNotificationSettings> Users => Set<UserNotificationSettings>();
    public DbSet<SourceEvent> SourceEvents => Set<SourceEvent>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("alerts", "public", table =>
            {
                table.HasCheckConstraint("ck_alerts_ids", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND owner_id <> '00000000-0000-0000-0000-000000000000'::uuid AND revision <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint("ck_alerts_name", $"name = btrim(name, {TrimCharacters}) AND char_length(name) BETWEEN 1 AND 120");
                table.HasCheckConstraint("ck_alerts_codes", "event_type = 'earthquake' AND condition_field = 'magnitude' AND condition_operator = 'gte' AND condition_value_type = 'number'");
                table.HasCheckConstraint("ck_alerts_finite_value", "condition_value > '-Infinity'::double precision AND condition_value < 'Infinity'::double precision");
            });
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(a => a.OwnerId).HasColumnName("owner_id").HasColumnType("uuid");
            entity.Property(a => a.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(a => a.EventType).HasColumnName("event_type").HasColumnType("text").IsRequired();
            entity.Property(a => a.Enabled).HasColumnName("enabled").HasColumnType("boolean").HasDefaultValue(false);
            entity.Property(a => a.Revision).HasColumnName("revision").HasColumnType("uuid").IsConcurrencyToken();
            entity.Property(a => a.ConditionField).HasColumnName("condition_field").HasColumnType("text").IsRequired();
            entity.Property(a => a.ConditionOperator).HasColumnName("condition_operator").HasColumnType("text").IsRequired();
            entity.Property(a => a.ConditionValueType).HasColumnName("condition_value_type").HasColumnType("text").IsRequired();
            entity.Property(a => a.ConditionValue).HasColumnName("condition_value").HasColumnType("double precision");
            entity.HasIndex(a => a.OwnerId).HasDatabaseName("ix_alerts_owner_id");
            entity.HasIndex(a => a.EventType).HasDatabaseName("ix_alerts_enabled_event_type").HasFilter("enabled = true");
            entity.HasOne(a => a.Owner).WithMany(u => u.Alerts)
                .HasForeignKey(a => a.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserNotificationSettings>(entity =>
        {
            entity.ToTable("users", "public", table =>
            {
                table.HasCheckConstraint("ck_users_ids", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND revision <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint("ck_users_destinations", $"(email_destination IS NOT NULL OR slack_destination IS NOT NULL) AND (email_destination IS NULL OR (email_destination = btrim(email_destination, {TrimCharacters}) AND char_length(email_destination) BETWEEN 1 AND 254)) AND (slack_destination IS NULL OR (slack_destination = btrim(slack_destination, {TrimCharacters}) AND char_length(slack_destination) BETWEEN 2 AND 80 AND slack_destination ~ '^[A-Z0-9]+$'))");
            });
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(u => u.EmailDestination).HasColumnName("email_destination").HasMaxLength(254);
            entity.Property(u => u.SlackDestination).HasColumnName("slack_destination").HasMaxLength(80);
            entity.Property(u => u.Revision).HasColumnName("revision").HasColumnType("uuid").IsConcurrencyToken();
        });

        modelBuilder.ConfigureRuntimeModel();
    }
}
