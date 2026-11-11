using Microsoft.AspNetCore.Identity;

namespace SevaSetu.Domain;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool WhatsAppOptOut { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Revoked { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

public class FamilyProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = "Delhi NCR";
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Elder> Elders { get; set; } = new List<Elder>();
}

public class Agency
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = "Delhi NCR";
    public string? Gstin { get; set; }
    public string? LegalName { get; set; }
    public string? Address { get; set; }
    public string Sac { get; set; } = "9993";
    public decimal CgstPercent { get; set; }
    public decimal SgstPercent { get; set; }
    public decimal IgstPercent { get; set; }
    public bool GstEnabled { get; set; }
    public decimal HourlyRate { get; set; } = 250m;
    public decimal LiveInDayRate { get; set; } = 1800m;
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<CaregiverProfile> Staff { get; set; } = new List<CaregiverProfile>();
}

public class Branch
{
    public int Id { get; set; }
    public int AgencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public Agency Agency { get; set; } = null!;
}

public class CaregiverProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? AgencyId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = "Delhi NCR";
    public CaregiverKind Kind { get; set; }
    public string Languages { get; set; } = "Hindi, English";
    public string? Gender { get; set; }
    public bool LiveInOk { get; set; } = true;
    public int Years { get; set; }
    public decimal HourlyRate { get; set; } = 300m;
    public decimal TwelveHourRate { get; set; } = 1800m;
    public decimal LiveInDayRate { get; set; } = 2000m;
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public string? KycReason { get; set; }
    public bool BackgroundCheckFlag { get; set; }
    public string? Gstin { get; set; }
    public string? LegalName { get; set; }
    public string? Address { get; set; }
    public string Sac { get; set; } = "9993";
    public decimal CgstPercent { get; set; }
    public decimal SgstPercent { get; set; }
    public decimal IgstPercent { get; set; }
    public bool GstEnabled { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Agency? Agency { get; set; }
    public ICollection<KycDocument> Documents { get; set; } = new List<KycDocument>();
}

public class KycDocument
{
    public int Id { get; set; }
    public int CaregiverId { get; set; }
    public string Kind { get; set; } = "Id";
    public string Label { get; set; } = string.Empty;
    public DateTime? ExpiresOn { get; set; }
    public CaregiverProfile Caregiver { get; set; } = null!;
}

public class HospitalOrg
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = "Delhi NCR";
}

public class HospitalCoordinator
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int HospitalOrgId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public HospitalOrg Hospital { get; set; } = null!;
}

public class Elder
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = "Delhi NCR";
    public string? Conditions { get; set; }
    public string? Allergies { get; set; }
    public string? EmergencyContact { get; set; }
    public bool ConsentHealth { get; set; }
    public bool ConsentPhotos { get; set; }
    public FamilyProfile Family { get; set; } = null!;
    public ICollection<CareEpisode> Episodes { get; set; } = new List<CareEpisode>();
}

public class CareEpisode
{
    public int Id { get; set; }
    public int ElderId { get; set; }
    public int? AgencyId { get; set; }
    public int? HospitalOrgId { get; set; }
    public DateTime StartDate { get; set; }
    public string PlanTemplate { get; set; } = "general";
    public string? PlanNotes { get; set; }
    public bool Active { get; set; } = true;
    public Elder Elder { get; set; } = null!;
    public Agency? Agency { get; set; }
    public HospitalPacket? HospitalPacket { get; set; }
}

public class HospitalPacket
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string Meds { get; set; } = string.Empty;
    public DateTime? FollowUpOn { get; set; }
    public CareEpisode Episode { get; set; } = null!;
}

public class CareRequest
{
    public int Id { get; set; }
    public int ElderId { get; set; }
    public int? CaregiverId { get; set; }
    public int? AgencyId { get; set; }
    public CareMode Mode { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Notes { get; set; }
    public string? Preferences { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public bool InstantBook { get; set; }
    public Elder Elder { get; set; } = null!;
    public CaregiverProfile? Caregiver { get; set; }
    public Agency? Agency { get; set; }
}

public class Assignment
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public int CaregiverId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;
    public CareEpisode Episode { get; set; } = null!;
    public CaregiverProfile Caregiver { get; set; } = null!;
}

public class Shift
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public int CaregiverId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Planned;
    public DateTime? ReminderSentAt { get; set; }
    public DateTime? ReplacementRequestedAt { get; set; }
    public DateTime? ReplacedAt { get; set; }
    public CareEpisode Episode { get; set; } = null!;
    public CaregiverProfile Caregiver { get; set; } = null!;
    public Visit? Visit { get; set; }
}

public class CalendarBlock
{
    public int Id { get; set; }
    public int CaregiverId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public CaregiverProfile Caregiver { get; set; } = null!;
}

public class Visit
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? Note { get; set; }
    public Shift Shift { get; set; } = null!;
}

public class CareLog
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public DateTime LogDate { get; set; }
    public int ComfortScore { get; set; }
    public string? Meals { get; set; }
    public string? Mobility { get; set; }
    public string? Note { get; set; }
    public CareEpisode Episode { get; set; } = null!;
}

public class AdlEntry
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public DateTime At { get; set; }
    public string Activity { get; set; } = string.Empty;
    public CareEpisode Episode { get; set; } = null!;
}

public class VitalLog
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public DateTime LogDate { get; set; }
    public decimal? Systolic { get; set; }
    public decimal? Diastolic { get; set; }
    public decimal? Pulse { get; set; }
    public decimal? TempC { get; set; }
    public decimal? Sugar { get; set; }
    public CareEpisode Episode { get; set; } = null!;
}

public class MarLine
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public string Drug { get; set; } = string.Empty;
    public string Dose { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public MarStatus Status { get; set; }
    public string? WitnessName { get; set; }
    public CareEpisode Episode { get; set; } = null!;
}

public class Incident
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public IncidentKind Kind { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime At { get; set; } = DateTime.UtcNow;
    public bool FamilyNotified { get; set; }
    public CareEpisode Episode { get; set; } = null!;
}

public class SosEvent
{
    public int Id { get; set; }
    public int ElderId { get; set; }
    public bool Ok { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public Elder Elder { get; set; } = null!;
}

public class Invoice
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public int? CaregiverId { get; set; }
    public int? AgencyId { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal HoursOrDays { get; set; }
    public decimal RateSnapshot { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public bool GstEnabledSnapshot { get; set; }
    public string? GstinSnapshot { get; set; }
    public string? LegalNameSnapshot { get; set; }
    public string SacSnapshot { get; set; } = "9993";
    public decimal CgstSnapshot { get; set; }
    public decimal SgstSnapshot { get; set; }
    public decimal IgstSnapshot { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public PaymentMode? PaymentMode { get; set; }
    public bool IsCommission { get; set; }
    public CareEpisode Episode { get; set; } = null!;
}

public class Rating
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public CareEpisode Episode { get; set; } = null!;
}

public class ConsumableIssue
{
    public int Id { get; set; }
    public int AgencyId { get; set; }
    public int ElderId { get; set; }
    public string Item { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public class AuditEvent
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? ElderId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public class PushSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class ReminderLog
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public string Channel { get; set; } = "whatsapp";
    public string Status { get; set; } = "skipped";
    public string? Detail { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
