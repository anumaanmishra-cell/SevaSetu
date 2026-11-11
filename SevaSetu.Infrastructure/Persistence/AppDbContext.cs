using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SevaSetu.Application.Common;

namespace SevaSetu.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FamilyProfile> Families => Set<FamilyProfile>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CaregiverProfile> Caregivers => Set<CaregiverProfile>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<HospitalOrg> Hospitals => Set<HospitalOrg>();
    public DbSet<HospitalCoordinator> HospitalCoordinators => Set<HospitalCoordinator>();
    public DbSet<Elder> Elders => Set<Elder>();
    public DbSet<CareEpisode> Episodes => Set<CareEpisode>();
    public DbSet<HospitalPacket> HospitalPackets => Set<HospitalPacket>();
    public DbSet<CareRequest> CareRequests => Set<CareRequest>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CalendarBlock> CalendarBlocks => Set<CalendarBlock>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<CareLog> CareLogs => Set<CareLog>();
    public DbSet<AdlEntry> AdlEntries => Set<AdlEntry>();
    public DbSet<VitalLog> VitalLogs => Set<VitalLog>();
    public DbSet<MarLine> MarLines => Set<MarLine>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<SosEvent> SosEvents => Set<SosEvent>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<ConsumableIssue> ConsumableIssues => Set<ConsumableIssue>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<FamilyProfile>(e =>
        {
            e.HasIndex(x => x.Phone).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
        builder.Entity<CaregiverProfile>(e =>
        {
            e.HasIndex(x => x.Phone);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Agency).WithMany(a => a.Staff).HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.TwelveHourRate).HasPrecision(18, 2);
            e.Property(x => x.LiveInDayRate).HasPrecision(18, 2);
            e.Property(x => x.CgstPercent).HasPrecision(5, 2);
            e.Property(x => x.SgstPercent).HasPrecision(5, 2);
            e.Property(x => x.IgstPercent).HasPrecision(5, 2);
        });
        builder.Entity<Agency>(e =>
        {
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.LiveInDayRate).HasPrecision(18, 2);
            e.Property(x => x.CgstPercent).HasPrecision(5, 2);
            e.Property(x => x.SgstPercent).HasPrecision(5, 2);
            e.Property(x => x.IgstPercent).HasPrecision(5, 2);
        });
        builder.Entity<HospitalCoordinator>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.Entity<HospitalPacket>().HasOne(x => x.Episode).WithOne(e => e.HospitalPacket).HasForeignKey<HospitalPacket>(x => x.EpisodeId);
        builder.Entity<Visit>().HasOne(x => x.Shift).WithOne(s => s.Visit).HasForeignKey<Visit>(x => x.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Assignment>(e =>
        {
            e.HasOne(x => x.Episode).WithMany().HasForeignKey(x => x.EpisodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Caregiver).WithMany().HasForeignKey(x => x.CaregiverId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Shift>(e =>
        {
            e.HasOne(x => x.Episode).WithMany().HasForeignKey(x => x.EpisodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Caregiver).WithMany().HasForeignKey(x => x.CaregiverId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CareRequest>(e =>
        {
            e.HasOne(x => x.Elder).WithMany().HasForeignKey(x => x.ElderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Caregiver).WithMany().HasForeignKey(x => x.CaregiverId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CareEpisode>(e =>
        {
            e.HasOne(x => x.Elder).WithMany(x => x.Episodes).HasForeignKey(x => x.ElderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CareLog>().HasIndex(x => new { x.EpisodeId, x.LogDate }).IsUnique();
        builder.Entity<VitalLog>().HasIndex(x => new { x.EpisodeId, x.LogDate }).IsUnique();
        builder.Entity<Invoice>(e =>
        {
            e.HasOne(x => x.Episode).WithMany().HasForeignKey(x => x.EpisodeId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RateSnapshot).HasPrecision(18, 2);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.Tax).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.HoursOrDays).HasPrecision(18, 2);
            e.Property(x => x.CgstSnapshot).HasPrecision(5, 2);
            e.Property(x => x.SgstSnapshot).HasPrecision(5, 2);
            e.Property(x => x.IgstSnapshot).HasPrecision(5, 2);
            e.Property(x => x.Number).HasMaxLength(32);
        });
        builder.Entity<VitalLog>(e =>
        {
            e.Property(x => x.Systolic).HasPrecision(8, 2);
            e.Property(x => x.Diastolic).HasPrecision(8, 2);
            e.Property(x => x.Pulse).HasPrecision(8, 2);
            e.Property(x => x.TempC).HasPrecision(8, 2);
            e.Property(x => x.Sugar).HasPrecision(8, 2);
        });
        builder.Entity<RefreshToken>().HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
        builder.Entity<Elder>().HasOne(x => x.Family).WithMany(f => f.Elders).HasForeignKey(x => x.FamilyId);
    }
}
