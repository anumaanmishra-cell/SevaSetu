namespace SevaSetu.Application.Dtos;

public record AuthRequest(string Email, string Password);
public record LoginByPhoneRequest(string Phone, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, string Role, string FullName, string Email);

public record RegisterFamilyRequest(string FullName, string Phone, string Password, string? City);
public record RegisterCaregiverRequest(string FullName, string Phone, string Password, string City, CaregiverKind Kind, int Years, string? Languages, string? Gender, decimal HourlyRate, bool LiveInOk);
public record RegisterAgencyRequest(string FullName, string Phone, string Password, string AgencyName, string City);
public record RegisterHospitalRequest(string FullName, string Phone, string Password, string HospitalName, string City);

public record KycDecisionRequest(KycStatus Status, string? Reason);
public record DisableUserRequest(string UserId, bool IsActive);

public record DirectoryCaregiverDto(int Id, string FullName, string City, CaregiverKind Kind, string Languages, string? Gender, decimal HourlyRate, decimal LiveInDayRate, bool LiveInOk, double? Rating, int? AgencyId, string? AgencyName);
public record DirectoryAgencyDto(int Id, string Name, string City, decimal HourlyRate, decimal LiveInDayRate);

public record ElderDto(int Id, string FullName, int Age, string Address, string City, string? Conditions, string? Allergies, string? EmergencyContact, bool ConsentHealth, bool ConsentPhotos);
public record ElderRequest(string FullName, int Age, string Address, string City, string? Conditions, string? Allergies, string? EmergencyContact, bool ConsentHealth, bool ConsentPhotos);

public record CareRequestDto(int Id, int ElderId, string ElderName, int? CaregiverId, int? AgencyId, CareMode Mode, DateTime StartAt, DateTime EndAt, string? Notes, RequestStatus Status, bool InstantBook);
public record CreateCareRequest(int ElderId, int? CaregiverId, int? AgencyId, CareMode Mode, DateTime StartAt, DateTime EndAt, string? Notes, string? Preferences, bool InstantBook);

public record EpisodeDto(int Id, int ElderId, string ElderName, int? AgencyId, string PlanTemplate, string? PlanNotes, bool Active, DateTime StartDate);
public record ShiftDto(int Id, int EpisodeId, int CaregiverId, string CaregiverName, DateTime SlotStart, DateTime SlotEnd, ShiftStatus Status, DateTime? ReplacementRequestedAt);
public record CalendarBlockDto(int Id, DateTime StartAt, DateTime EndAt);
public record PhotoDto(int Id, string Kind, string Url, DateTime At);
public record ConsumableDto(int Id, int ElderId, string Item, int Quantity, DateTime At);
public record VapidPublicDto(string? PublicKey);
public record VisitDto(int Id, int ShiftId, VisitStatus Status, DateTime? CheckInAt, DateTime? CheckOutAt, string? Note);
public record CareLogDto(int Id, DateTime LogDate, int ComfortScore, string? Meals, string? Mobility, string? Note);
public record VitalDto(int Id, DateTime LogDate, decimal? Systolic, decimal? Diastolic, decimal? Pulse, decimal? TempC, decimal? Sugar);
public record AdlDto(int Id, DateTime At, string Activity);
public record MarDto(int Id, string Drug, string Dose, DateTime ScheduledAt, MarStatus Status, string? WitnessName);
public record IncidentDto(int Id, IncidentKind Kind, string Note, DateTime At);
public record InvoiceDto(int Id, string Number, decimal Total, decimal Tax, PaymentStatus PaymentStatus, PaymentMode? PaymentMode, bool GstEnabledSnapshot, DateTime IssuedAt, bool IsCommission);
public record HospitalPacketDto(string Diagnosis, string Meds, DateTime? FollowUpOn);
public record BranchDto(int Id, string Name, string City);
public record StaffDto(int Id, string FullName, CaregiverKind Kind, KycStatus KycStatus);
public record OccupancyDto(int HoursRostered, int HoursBilled);
public record AuditDto(int Id, string UserId, int? ElderId, string Action, DateTime At);
public record RatingDto(int Stars, string? Comment);
public record SosDto(int ElderId, bool Ok);

public record SaveCareLogRequest(int ComfortScore, string? Meals, string? Mobility, string? Note);
public record SaveVitalRequest(decimal? Systolic, decimal? Diastolic, decimal? Pulse, decimal? TempC, decimal? Sugar);
public record AddAdlRequest(string Activity);
public record AddMarRequest(string Drug, string Dose, DateTime ScheduledAt, MarStatus Status, string? WitnessName);
public record AddIncidentRequest(IncidentKind Kind, string Note);
public record IssueInvoiceRequest(DateTime PeriodStart, DateTime PeriodEnd, decimal HoursOrDays, bool LiveIn);
public record PayInvoiceRequest(PaymentMode Mode);
public record UpdatePlanRequest(string PlanTemplate, string? PlanNotes);
public record BlockCalendarRequest(DateTime StartAt, DateTime EndAt);
public record ReplaceShiftRequest(int NewCaregiverId);
public record PushSubscribeRequest(string Endpoint, string P256dh, string Auth);
public record RateRequest(int Stars, string? Comment);
public record ConsumableRequest(int ElderId, string Item, int Quantity);
public record HospitalPacketRequest(int ElderId, string Diagnosis, string Meds, DateTime? FollowUpOn, int? AgencyId, int? CaregiverId);
public record GstSettingsRequest(bool GstEnabled, string? Gstin, string? LegalName, string? Address, decimal CgstPercent, decimal SgstPercent, decimal IgstPercent);
public record BranchRequest(string Name, string City);
public record CheckInRequest(string? Note);
