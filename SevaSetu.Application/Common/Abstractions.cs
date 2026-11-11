using Microsoft.EntityFrameworkCore;

namespace SevaSetu.Application.Common;

public interface IAppDbContext
{
    DbSet<FamilyProfile> Families { get; }
    DbSet<Agency> Agencies { get; }
    DbSet<Branch> Branches { get; }
    DbSet<CaregiverProfile> Caregivers { get; }
    DbSet<KycDocument> KycDocuments { get; }
    DbSet<HospitalOrg> Hospitals { get; }
    DbSet<HospitalCoordinator> HospitalCoordinators { get; }
    DbSet<Elder> Elders { get; }
    DbSet<CareEpisode> Episodes { get; }
    DbSet<HospitalPacket> HospitalPackets { get; }
    DbSet<CareRequest> CareRequests { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<CalendarBlock> CalendarBlocks { get; }
    DbSet<Visit> Visits { get; }
    DbSet<CareLog> CareLogs { get; }
    DbSet<AdlEntry> AdlEntries { get; }
    DbSet<VitalLog> VitalLogs { get; }
    DbSet<MarLine> MarLines { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<SosEvent> SosEvents { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Rating> Ratings { get; }
    DbSet<ConsumableIssue> ConsumableIssues { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    DbSet<PushSubscription> PushSubscriptions { get; }
    DbSet<ReminderLog> ReminderLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class AppException : Exception
{
    public int StatusCode { get; }
    public AppException(string message, int statusCode = 400) : base(message) => StatusCode = statusCode;
}

public interface ICurrentUser
{
    string? UserId { get; }
    string? Role { get; }
}

public static class IndiaTime
{
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
        }
        return TimeZoneInfo.Utc;
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
    public static DateTime Today => Now.Date;
    public static DateTime ToUtc(DateTime localUnspecified) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified), Zone);
}
