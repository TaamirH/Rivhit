using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rivhit.Api.Attendance;
using Rivhit.Api.Audit;

namespace Rivhit.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Punch> Punches => Set<Punch>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var isSqlServer = Database.IsSqlServer();

        builder.Entity<Shift>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();

            // Enforce: at most one open shift per user.
            b.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[ClosedAtUtc] IS NULL");
        });

        builder.Entity<Punch>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();

            b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Note).HasMaxLength(4000);

            b.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
            b.Property(x => x.UtcOffset).HasMaxLength(16).IsRequired();

            var raw = b.Property(x => x.TimeApiRawResponse).IsRequired();
            if (isSqlServer)
            {
                raw.HasColumnType("nvarchar(max)");
            }

            b.HasOne(x => x.Shift)
                .WithMany(s => s.Punches)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        });

        builder.Entity<AuditLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ActorUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Action).HasMaxLength(200).IsRequired();
            b.Property(x => x.TargetUserId).HasMaxLength(450);
            var details = b.Property(x => x.DetailsJson).IsRequired();
            if (isSqlServer)
            {
                details.HasColumnType("nvarchar(max)");
            }

            b.HasIndex(x => x.CreatedAtUtc);
        });

        builder.Entity<IdempotencyKey>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Key).HasMaxLength(200).IsRequired();

            b.HasIndex(x => new { x.UserId, x.Key }).IsUnique();

            b.HasOne(x => x.Punch)
                .WithMany()
                .HasForeignKey(x => x.PunchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

