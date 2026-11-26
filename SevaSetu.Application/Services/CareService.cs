using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SevaSetu.Application.Common;
using SevaSetu.Application.Dtos;

namespace SevaSetu.Application.Services;

public class CareService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPhotoStore _photos;

    public CareService(IAppDbContext db, ICurrentUser current, UserManager<ApplicationUser> users, IPhotoStore photos)
    {
        _db = db;
        _current = current;
        _users = users;
        _photos = photos;
    }

    public async Task AuditAsync(string action, int? elderId, CancellationToken ct)
    {
        _db.AuditEvents.Add(new AuditEvent { UserId = _current.UserId ?? "", ElderId = elderId, Action = action });
        await _db.SaveChangesAsync(ct);
    }

    private string RequireUser() => _current.UserId ?? throw new AppException("Unauthorized.", 401);

    public async Task<FamilyProfile> RequireFamilyAsync(CancellationToken ct)
    {
        var id = RequireUser();
        return await _db.Families.Include(f => f.User).FirstOrDefaultAsync(f => f.UserId == id, ct)
            ?? throw new AppException("Family profile not found.", 403);
    }

    public async Task<CaregiverProfile> RequireCaregiverAsync(CancellationToken ct)
    {
        var id = RequireUser();
        return await _db.Caregivers.Include(c => c.User).Include(c => c.Agency)
            .FirstOrDefaultAsync(c => c.UserId == id, ct)
            ?? throw new AppException("Caregiver profile not found.", 403);
    }

    public async Task<Agency> RequireAgencyAsync(CancellationToken ct)
    {
        var caregiver = await RequireCaregiverAsync(ct);
        if (caregiver.AgencyId is null) throw new AppException("Agency login required.", 403);
        return await _db.Agencies.FirstAsync(a => a.Id == caregiver.AgencyId, ct);
    }

    public async Task<HospitalCoordinator> RequireHospitalAsync(CancellationToken ct)
    {
        var id = RequireUser();
        return await _db.HospitalCoordinators.Include(h => h.Hospital)
            .FirstOrDefaultAsync(h => h.UserId == id, ct)
            ?? throw new AppException("Hospital login required.", 403);
    }

    public void RequireAdmin()
    {
        if (_current.Role != AppRoles.PlatformAdmin) throw new AppException("Admin only.", 403);
    }

    public async Task<List<DirectoryCaregiverDto>> SearchCaregiversAsync(string? city, CaregiverKind? kind, CancellationToken ct)
    {
        var q = _db.Caregivers.Include(c => c.User).Include(c => c.Agency)
            .Where(c => c.KycStatus == KycStatus.Passed && c.User.IsActive && c.AgencyId == null);
        if (!string.IsNullOrWhiteSpace(city)) q = q.Where(c => c.City.Contains(city));
        if (kind is not null) q = q.Where(c => c.Kind == kind);
        var list = await q.Take(50).ToListAsync(ct);
        var cgIds = list.Select(c => c.Id).ToList();
        var assign = await _db.Assignments.Where(a => cgIds.Contains(a.CaregiverId)).ToListAsync(ct);
        var epIds = assign.Select(a => a.EpisodeId).Distinct().ToList();
        var ratings = await _db.Ratings.Where(r => epIds.Contains(r.EpisodeId)).ToListAsync(ct);
        return list.Select(c =>
        {
            var myEps = assign.Where(a => a.CaregiverId == c.Id).Select(a => a.EpisodeId).ToHashSet();
            var mine = ratings.Where(r => myEps.Contains(r.EpisodeId)).Select(r => r.Stars).ToList();
            double? avg = mine.Count == 0 ? null : Math.Round(mine.Average(), 1);
            return new DirectoryCaregiverDto(c.Id, c.User.FullName, c.City, c.Kind, c.Languages, c.Gender, c.HourlyRate, c.LiveInDayRate, c.LiveInOk, avg, null, null);
        }).ToList();
    }

    public async Task<List<DirectoryAgencyDto>> SearchAgenciesAsync(string? city, CancellationToken ct)
    {
        var q = _db.Agencies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(city)) q = q.Where(a => a.City.Contains(city));
        return await q.Select(a => new DirectoryAgencyDto(a.Id, a.Name, a.City, a.HourlyRate, a.LiveInDayRate)).Take(50).ToListAsync(ct);
    }

    public async Task<ElderDto> AddElderAsync(ElderRequest req, CancellationToken ct)
    {
        var family = await RequireFamilyAsync(ct);
        if (!req.ConsentHealth) throw new AppException("Health-data consent is required.");
        var elder = new Elder
        {
            FamilyId = family.Id,
            FullName = req.FullName.Trim(),
            Age = req.Age,
            Address = req.Address.Trim(),
            City = req.City.Trim(),
            Conditions = req.Conditions,
            Allergies = req.Allergies,
            EmergencyContact = req.EmergencyContact,
            ConsentHealth = req.ConsentHealth,
            ConsentPhotos = req.ConsentPhotos
        };
        _db.Elders.Add(elder);
        await _db.SaveChangesAsync(ct);
        await AuditAsync("elder.create", elder.Id, ct);
        return MapElder(elder);
    }

    public async Task<List<ElderDto>> MyEldersAsync(CancellationToken ct)
    {
        var family = await RequireFamilyAsync(ct);
        return await _db.Elders.Where(e => e.FamilyId == family.Id).Select(e => MapElder(e)).ToListAsync(ct);
    }

    public async Task<CareRequestDto> CreateRequestAsync(CreateCareRequest req, CancellationToken ct)
    {
        var family = await RequireFamilyAsync(ct);
        var elder = await _db.Elders.FirstOrDefaultAsync(e => e.Id == req.ElderId && e.FamilyId == family.Id, ct)
            ?? throw new AppException("Elder not found.", 404);
        if (req.EndAt <= req.StartAt) throw new AppException("End must be after start.");

        if (req.InstantBook)
        {
            if (req.CaregiverId is not int cid) throw new AppException("Choose a caregiver to instant-book.");
            var cg = await _db.Caregivers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == cid && c.KycStatus == KycStatus.Passed, ct)
                ?? throw new AppException("Caregiver is not available.");
            if (await OverlapsAsync(cid, req.StartAt, req.EndAt, ct))
                throw new AppException("That slot is not free.");
            var episode = await EnsureEpisodeAsync(elder.Id, null, ct);
            await AssignAndShiftAsync(episode.Id, cid, req.StartAt, req.EndAt, ct);
            var booked = new CareRequest
            {
                ElderId = elder.Id, CaregiverId = cid, Mode = req.Mode, StartAt = req.StartAt, EndAt = req.EndAt,
                Notes = req.Notes, Preferences = req.Preferences, Status = RequestStatus.Booked, InstantBook = true
            };
            _db.CareRequests.Add(booked);
            await _db.SaveChangesAsync(ct);
            return MapRequest(booked, elder.FullName);
        }

        var row = new CareRequest
        {
            ElderId = elder.Id, CaregiverId = req.CaregiverId, AgencyId = req.AgencyId, Mode = req.Mode,
            StartAt = req.StartAt, EndAt = req.EndAt, Notes = req.Notes, Preferences = req.Preferences,
            Status = RequestStatus.Open, InstantBook = false
        };
        _db.CareRequests.Add(row);
        await _db.SaveChangesAsync(ct);
        return MapRequest(row, elder.FullName);
    }

    public async Task<List<CareRequestDto>> InboxAsync(CancellationToken ct)
    {
        if (_current.Role == AppRoles.Caregiver)
        {
            var cg = await RequireCaregiverAsync(ct);
            if (cg.AgencyId is not null) return await AgencyInboxAsync(ct);
            return await _db.CareRequests.Include(r => r.Elder)
                .Where(r => r.CaregiverId == cg.Id || (r.CaregiverId == null && r.AgencyId == null && r.Status == RequestStatus.Open))
                .OrderByDescending(r => r.Id).Take(50)
                .Select(r => MapRequest(r, r.Elder.FullName)).ToListAsync(ct);
        }
        if (_current.Role == AppRoles.AgencyAdmin)
            return await AgencyInboxAsync(ct);
        var family = await RequireFamilyAsync(ct);
        return await _db.CareRequests.Include(r => r.Elder)
            .Where(r => r.Elder.FamilyId == family.Id)
            .OrderByDescending(r => r.Id).Take(50)
            .Select(r => MapRequest(r, r.Elder.FullName)).ToListAsync(ct);
    }

    private async Task<List<CareRequestDto>> AgencyInboxAsync(CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        return await _db.CareRequests.Include(r => r.Elder)
            .Where(r => r.AgencyId == agency.Id || (r.AgencyId == null && r.CaregiverId == null && r.Status == RequestStatus.Open))
            .OrderByDescending(r => r.Id).Take(50)
            .Select(r => MapRequest(r, r.Elder.FullName)).ToListAsync(ct);
    }

    public async Task RespondRequestAsync(int id, bool accept, int? staffCaregiverId, CancellationToken ct)
    {
        var req = await _db.CareRequests.Include(r => r.Elder).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new AppException("Request not found.", 404);
        if (req.Status != RequestStatus.Open) throw new AppException("Request is no longer open.");
        if (!accept)
        {
            req.Status = RequestStatus.Declined;
            await _db.SaveChangesAsync(ct);
            return;
        }
        int caregiverId;
        int? agencyId = null;
        if (_current.Role == AppRoles.AgencyAdmin)
        {
            var agency = await RequireAgencyAsync(ct);
            agencyId = agency.Id;
            caregiverId = staffCaregiverId ?? throw new AppException("Assign a staff caregiver.");
            var staff = await _db.Caregivers.FirstOrDefaultAsync(c => c.Id == caregiverId && c.AgencyId == agency.Id, ct)
                ?? throw new AppException("Staff not found.");
            if (staff.KycStatus != KycStatus.Passed) throw new AppException("Staff KYC is not passed.");
        }
        else
        {
            var cg = await RequireCaregiverAsync(ct);
            if (cg.KycStatus != KycStatus.Passed) throw new AppException("KYC must be passed before accepting work.");
            caregiverId = cg.Id;
        }
        if (await OverlapsAsync(caregiverId, req.StartAt, req.EndAt, ct))
            throw new AppException("That slot overlaps another shift.");
        var episode = await EnsureEpisodeAsync(req.ElderId, agencyId, ct);
        await AssignAndShiftAsync(episode.Id, caregiverId, req.StartAt, req.EndAt, ct);
        req.Status = RequestStatus.Accepted;
        req.CaregiverId = caregiverId;
        req.AgencyId = agencyId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<EpisodeDto>> EpisodesAsync(CancellationToken ct)
    {
        IQueryable<CareEpisode> q = _db.Episodes.Include(e => e.Elder);
        if (_current.Role == AppRoles.Family)
        {
            var family = await RequireFamilyAsync(ct);
            q = q.Where(e => e.Elder.FamilyId == family.Id);
        }
        else if (_current.Role == AppRoles.Caregiver)
        {
            var cg = await RequireCaregiverAsync(ct);
            if (cg.AgencyId is int aid)
                q = q.Where(e => e.AgencyId == aid || _db.Assignments.Any(a => a.EpisodeId == e.Id && a.CaregiverId == cg.Id && a.Active));
            else
                q = q.Where(e => _db.Assignments.Any(a => a.EpisodeId == e.Id && a.CaregiverId == cg.Id && a.Active));
        }
        else if (_current.Role == AppRoles.HospitalCoordinator)
        {
            var h = await RequireHospitalAsync(ct);
            q = q.Where(e => e.HospitalOrgId == h.HospitalOrgId);
        }
        else if (_current.Role == AppRoles.AgencyAdmin)
        {
            var agency = await RequireAgencyAsync(ct);
            q = q.Where(e => e.AgencyId == agency.Id);
        }
        else RequireAdmin();
        return await q.OrderByDescending(e => e.Id).Take(50)
            .Select(e => new EpisodeDto(e.Id, e.ElderId, e.Elder.FullName, e.AgencyId, e.PlanTemplate, e.PlanNotes, e.Active, e.StartDate))
            .ToListAsync(ct);
    }

    public async Task<EpisodeDto> GetEpisodeAsync(int id, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(id, ct);
        var e = await _db.Episodes.Include(x => x.Elder).FirstAsync(x => x.Id == id, ct);
        await AuditAsync("episode.open", e.ElderId, ct);
        return new EpisodeDto(e.Id, e.ElderId, e.Elder.FullName, e.AgencyId, e.PlanTemplate, e.PlanNotes, e.Active, e.StartDate);
    }

    public async Task UpdatePlanAsync(int episodeId, UpdatePlanRequest req, CancellationToken ct)
    {
        var e = await EnsureEpisodeAccessAsync(episodeId, ct);
        if (_current.Role is not (AppRoles.AgencyAdmin or AppRoles.Caregiver or AppRoles.HospitalCoordinator))
            throw new AppException("Only the care team can edit the plan.", 403);
        e.PlanTemplate = string.IsNullOrWhiteSpace(req.PlanTemplate) ? "general" : req.PlanTemplate.Trim();
        e.PlanNotes = req.PlanNotes;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ShiftDto>> ShiftsAsync(int? episodeId, CancellationToken ct)
    {
        var q = _db.Shifts.Include(s => s.Caregiver).ThenInclude(c => c.User).Include(s => s.Episode).ThenInclude(e => e.Elder).AsQueryable();
        if (episodeId is int eid)
        {
            await EnsureEpisodeAccessAsync(eid, ct);
            q = q.Where(s => s.EpisodeId == eid);
        }
        else if (_current.Role == AppRoles.Caregiver)
        {
            var cg = await RequireCaregiverAsync(ct);
            q = q.Where(s => s.CaregiverId == cg.Id);
        }
        else if (_current.Role == AppRoles.Family)
        {
            var family = await RequireFamilyAsync(ct);
            q = q.Where(s => s.Episode.Elder.FamilyId == family.Id);
        }
        else if (_current.Role == AppRoles.AgencyAdmin)
        {
            var agency = await RequireAgencyAsync(ct);
            q = q.Where(s => s.Caregiver.AgencyId == agency.Id || s.Episode.AgencyId == agency.Id);
        }
        return await q.OrderBy(s => s.SlotStart).Take(100)
            .Select(s => new ShiftDto(s.Id, s.EpisodeId, s.CaregiverId, s.Caregiver.User.FullName, s.SlotStart, s.SlotEnd, s.Status, s.ReplacementRequestedAt))
            .ToListAsync(ct);
    }

    public async Task CheckInAsync(int shiftId, string? note, CancellationToken ct)
    {
        var cg = await RequireCaregiverAsync(ct);
        var shift = await _db.Shifts.Include(s => s.Visit).FirstOrDefaultAsync(s => s.Id == shiftId && s.CaregiverId == cg.Id, ct)
            ?? throw new AppException("Shift not found.", 404);
        if (await _db.Visits.AnyAsync(v => v.Shift.CaregiverId == cg.Id && v.Status == VisitStatus.CheckedIn, ct))
            throw new AppException("Finish the open visit before checking in again.");
        var visit = shift.Visit ?? new Visit { ShiftId = shift.Id, Status = VisitStatus.Scheduled };
        if (shift.Visit is null) _db.Visits.Add(visit);
        visit.Status = VisitStatus.CheckedIn;
        visit.CheckInAt = DateTime.UtcNow;
        visit.Note = note;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CheckOutAsync(int shiftId, string? note, CancellationToken ct)
    {
        var cg = await RequireCaregiverAsync(ct);
        var shift = await _db.Shifts.Include(s => s.Visit).FirstOrDefaultAsync(s => s.Id == shiftId && s.CaregiverId == cg.Id, ct)
            ?? throw new AppException("Shift not found.", 404);
        if (shift.Visit is null || shift.Visit.Status != VisitStatus.CheckedIn)
            throw new AppException("Check in first.");
        shift.Visit.Status = VisitStatus.CheckedOut;
        shift.Visit.CheckOutAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(note)) shift.Visit.Note = note;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkNoShowAsync(int shiftId, CancellationToken ct)
    {
        var shift = await _db.Shifts.Include(s => s.Episode).ThenInclude(e => e.Elder).Include(s => s.Visit)
            .FirstOrDefaultAsync(s => s.Id == shiftId, ct) ?? throw new AppException("Shift not found.", 404);
        await EnsureEpisodeAccessAsync(shift.EpisodeId, ct);
        if (_current.Role is not (AppRoles.Family or AppRoles.AgencyAdmin))
            throw new AppException("Family or agency can mark a no-show.", 403);
        var visit = shift.Visit ?? new Visit { ShiftId = shift.Id };
        if (shift.Visit is null) _db.Visits.Add(visit);
        visit.Status = VisitStatus.NoShow;
        shift.ReplacementRequestedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReplaceShiftAsync(int shiftId, int newCaregiverId, CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId, ct) ?? throw new AppException("Shift not found.", 404);
        var staff = await _db.Caregivers.FirstOrDefaultAsync(c => c.Id == newCaregiverId && c.AgencyId == agency.Id, ct)
            ?? throw new AppException("Staff not found.");
        if (await OverlapsAsync(newCaregiverId, shift.SlotStart, shift.SlotEnd, ct))
            throw new AppException("Replacement overlaps another shift.");
        shift.Status = ShiftStatus.Replaced;
        shift.ReplacedAt = DateTime.UtcNow;
        await AssignAndShiftAsync(shift.EpisodeId, staff.Id, shift.SlotStart, shift.SlotEnd, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BlockCalendarAsync(BlockCalendarRequest req, CancellationToken ct)
    {
        var cg = await RequireCaregiverAsync(ct);
        _db.CalendarBlocks.Add(new CalendarBlock { CaregiverId = cg.Id, StartAt = req.StartAt, EndAt = req.EndAt });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<CalendarBlockDto>> CalendarBlocksAsync(CancellationToken ct)
    {
        var cg = await RequireCaregiverAsync(ct);
        return await _db.CalendarBlocks.Where(b => b.CaregiverId == cg.Id).OrderByDescending(b => b.StartAt)
            .Select(b => new CalendarBlockDto(b.Id, b.StartAt, b.EndAt)).ToListAsync(ct);
    }

    public async Task SaveLogAsync(int episodeId, SaveCareLogRequest req, CancellationToken ct)
    {
        if (req.ComfortScore is < 1 or > 10) throw new AppException("Comfort score must be 1 to 10.");
        await EnsureEpisodeAccessAsync(episodeId, ct);
        var day = IndiaTime.Today;
        var log = await _db.CareLogs.FirstOrDefaultAsync(l => l.EpisodeId == episodeId && l.LogDate == day, ct);
        if (log is null)
        {
            log = new CareLog { EpisodeId = episodeId, LogDate = day };
            _db.CareLogs.Add(log);
        }
        log.ComfortScore = req.ComfortScore;
        log.Meals = req.Meals;
        log.Mobility = req.Mobility;
        log.Note = req.Note is { Length: > 200 } ? req.Note[..200] : req.Note;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<CareLogDto>> LogsAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        var from = IndiaTime.Today.AddDays(-14);
        return await _db.CareLogs.Where(l => l.EpisodeId == episodeId && l.LogDate >= from)
            .OrderByDescending(l => l.LogDate)
            .Select(l => new CareLogDto(l.Id, l.LogDate, l.ComfortScore, l.Meals, l.Mobility, l.Note))
            .ToListAsync(ct);
    }

    public async Task SaveVitalAsync(int episodeId, SaveVitalRequest req, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        var day = IndiaTime.Today;
        var row = await _db.VitalLogs.FirstOrDefaultAsync(v => v.EpisodeId == episodeId && v.LogDate == day, ct);
        if (row is null)
        {
            row = new VitalLog { EpisodeId = episodeId, LogDate = day };
            _db.VitalLogs.Add(row);
        }
        row.Systolic = req.Systolic; row.Diastolic = req.Diastolic; row.Pulse = req.Pulse; row.TempC = req.TempC; row.Sugar = req.Sugar;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<VitalDto>> VitalsAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        return await _db.VitalLogs.Where(v => v.EpisodeId == episodeId).OrderByDescending(v => v.LogDate).Take(30)
            .Select(v => new VitalDto(v.Id, v.LogDate, v.Systolic, v.Diastolic, v.Pulse, v.TempC, v.Sugar)).ToListAsync(ct);
    }

    public async Task AddAdlAsync(int episodeId, string activity, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        _db.AdlEntries.Add(new AdlEntry { EpisodeId = episodeId, At = DateTime.UtcNow, Activity = activity.Trim() });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<AdlDto>> AdlAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        return await _db.AdlEntries.Where(a => a.EpisodeId == episodeId).OrderByDescending(a => a.At).Take(50)
            .Select(a => new AdlDto(a.Id, a.At, a.Activity)).ToListAsync(ct);
    }

    public async Task AddMarAsync(int episodeId, AddMarRequest req, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        _db.MarLines.Add(new MarLine
        {
            EpisodeId = episodeId, Drug = req.Drug.Trim(), Dose = req.Dose.Trim(),
            ScheduledAt = req.ScheduledAt, Status = req.Status, WitnessName = req.WitnessName
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MarDto>> MarAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        return await _db.MarLines.Where(m => m.EpisodeId == episodeId).OrderByDescending(m => m.ScheduledAt).Take(50)
            .Select(m => new MarDto(m.Id, m.Drug, m.Dose, m.ScheduledAt, m.Status, m.WitnessName)).ToListAsync(ct);
    }

    public async Task AddIncidentAsync(int episodeId, AddIncidentRequest req, CancellationToken ct)
    {
        var e = await EnsureEpisodeAccessAsync(episodeId, ct);
        _db.Incidents.Add(new Incident { EpisodeId = episodeId, Kind = req.Kind, Note = req.Note.Trim(), FamilyNotified = true });
        await _db.SaveChangesAsync(ct);
        await AuditAsync("incident", e.ElderId, ct);
    }

    public async Task<List<IncidentDto>> IncidentsAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        return await _db.Incidents.Where(i => i.EpisodeId == episodeId).OrderByDescending(i => i.At)
            .Select(i => new IncidentDto(i.Id, i.Kind, i.Note, i.At)).ToListAsync(ct);
    }

    public async Task SosAsync(int elderId, bool ok, CancellationToken ct)
    {
        if (_current.Role == AppRoles.Family)
        {
            var family = await RequireFamilyAsync(ct);
            if (!await _db.Elders.AnyAsync(e => e.Id == elderId && e.FamilyId == family.Id, ct))
                throw new AppException("Elder not found.", 404);
        }
        else if (_current.Role == AppRoles.Elder)
        {
            var mine = await MyElderAsync(ct);
            if (mine.Id != elderId) throw new AppException("Elder not found.", 404);
        }
        else throw new AppException("SOS is for family or the elder.", 403);
        _db.SosEvents.Add(new SosEvent { ElderId = elderId, Ok = ok });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ElderDto> MyElderAsync(CancellationToken ct)
    {
        var id = RequireUser();
        var elder = await _db.Elders.FirstOrDefaultAsync(e => e.UserId == id, ct)
            ?? throw new AppException("Elder login is not linked.", 404);
        return MapElder(elder);
    }

    public async Task<InvoiceDto> IssueInvoiceAsync(int episodeId, IssueInvoiceRequest req, CancellationToken ct)
    {
        var episode = await EnsureEpisodeAccessAsync(episodeId, ct);
        decimal rate;
        int? caregiverId = null;
        int? agencyId = episode.AgencyId;
        bool gst; string? gstin; string? legal; string sac; decimal cgst, sgst, igst;
        if (agencyId is int aid)
        {
            var agency = await _db.Agencies.FirstAsync(a => a.Id == aid, ct);
            rate = req.LiveIn ? agency.LiveInDayRate : agency.HourlyRate;
            gst = agency.GstEnabled; gstin = agency.Gstin; legal = agency.LegalName; sac = agency.Sac;
            cgst = agency.CgstPercent; sgst = agency.SgstPercent; igst = agency.IgstPercent;
        }
        else
        {
            var assign = await _db.Assignments.Include(a => a.Caregiver).FirstOrDefaultAsync(a => a.EpisodeId == episodeId && a.Active, ct)
                ?? throw new AppException("No caregiver assigned.");
            var cg = assign.Caregiver;
            caregiverId = cg.Id;
            rate = req.LiveIn ? cg.LiveInDayRate : cg.HourlyRate;
            gst = cg.GstEnabled; gstin = cg.Gstin; legal = cg.LegalName; sac = cg.Sac;
            cgst = cg.CgstPercent; sgst = cg.SgstPercent; igst = cg.IgstPercent;
        }
        var sub = Math.Round(req.HoursOrDays * rate, 2);
        var taxPct = gst ? (igst > 0 ? igst : cgst + sgst) : 0;
        var tax = Math.Round(sub * taxPct / 100m, 2);
        var inv = new Invoice
        {
            EpisodeId = episodeId, CaregiverId = caregiverId, AgencyId = agencyId,
            PeriodStart = req.PeriodStart, PeriodEnd = req.PeriodEnd, HoursOrDays = req.HoursOrDays,
            RateSnapshot = rate, SubTotal = sub, Tax = tax, Total = sub + tax,
            GstEnabledSnapshot = gst, GstinSnapshot = gstin, LegalNameSnapshot = legal, SacSnapshot = sac,
            CgstSnapshot = cgst, SgstSnapshot = sgst, IgstSnapshot = igst
        };
        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync(ct);
        inv.Number = $"SS-{IndiaTime.Today:yyyy}-{inv.Id:0000}";
        await _db.SaveChangesAsync(ct);
        return MapInvoice(inv);
    }

    public async Task PayInvoiceAsync(int id, PaymentMode mode, CancellationToken ct)
    {
        var inv = await _db.Invoices.Include(i => i.Episode).ThenInclude(e => e.Elder).FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new AppException("Invoice not found.", 404);
        await EnsureEpisodeAccessAsync(inv.EpisodeId, ct);
        inv.PaymentStatus = PaymentStatus.Paid;
        inv.PaymentMode = mode;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<InvoiceDto>> InvoicesAsync(int? episodeId, CancellationToken ct)
    {
        var q = _db.Invoices.Include(i => i.Episode).ThenInclude(e => e.Elder).AsQueryable();
        if (episodeId is int eid)
        {
            await EnsureEpisodeAccessAsync(eid, ct);
            q = q.Where(i => i.EpisodeId == eid);
        }
        else if (_current.Role == AppRoles.Family)
        {
            var family = await RequireFamilyAsync(ct);
            q = q.Where(i => i.Episode.Elder.FamilyId == family.Id);
        }
        return await q.OrderByDescending(i => i.Id).Take(50).Select(i => MapInvoice(i)).ToListAsync(ct);
    }

    public async Task<string> InvoiceHtmlAsync(int id, CancellationToken ct)
    {
        var inv = await _db.Invoices.Include(i => i.Episode).ThenInclude(e => e.Elder).ThenInclude(e => e.Family).ThenInclude(f => f.User)
            .FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new AppException("Invoice not found.", 404);
        await EnsureEpisodeAccessAsync(inv.EpisodeId, ct);
        var culture = System.Globalization.CultureInfo.GetCultureInfo("en-IN");
        var taxNote = inv.GstEnabledSnapshot
            ? $"GSTIN {inv.GstinSnapshot} · SAC {inv.SacSnapshot} · tax ₹{inv.Tax.ToString("N2", culture)}"
            : "AaoSaharaBane home-care bill. This is a record of the fee. No tax is charged on this invoice.";
        var payment = inv.PaymentStatus == PaymentStatus.Paid
            ? $"Paid · {(inv.PaymentMode == PaymentMode.Upi ? "UPI" : "Cash")}"
            : "Unpaid";
        return $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>{inv.Number}</title></head><body>" +
               $"<h1>AaoSaharaBane {(inv.GstEnabledSnapshot ? "TAX INVOICE" : "INVOICE")}</h1>" +
               $"<p>{taxNote}</p><p>{inv.Number} · {payment}</p>" +
               $"<p>Bill to: {inv.Episode.Elder.Family.User.FullName} · Elder: {inv.Episode.Elder.FullName}</p>" +
               $"<p>From: {inv.LegalNameSnapshot ?? "AaoSaharaBane caregiver"}</p>" +
               $"<p>Qty {inv.HoursOrDays} × ₹{inv.RateSnapshot.ToString("N2", culture)} = ₹{inv.SubTotal.ToString("N2", culture)}</p>" +
               $"<p>Total ₹{inv.Total.ToString("N2", culture)}</p></body></html>";
    }

    public async Task IssueCommissionAsync(int invoiceId, CancellationToken ct)
    {
        RequireAdmin();
        var inv = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct) ?? throw new AppException("Invoice not found.", 404);
        if (inv.IsCommission) throw new AppException("Already a commission bill.");
        var commission = Math.Round(inv.SubTotal * 0.10m, 2);
        var row = new Invoice
        {
            EpisodeId = inv.EpisodeId, AgencyId = inv.AgencyId, CaregiverId = inv.CaregiverId,
            PeriodStart = inv.PeriodStart, PeriodEnd = inv.PeriodEnd, HoursOrDays = 1,
            RateSnapshot = commission, SubTotal = commission, Tax = 0, Total = commission,
            GstEnabledSnapshot = false, IsCommission = true, Number = $"SS-C-{inv.Id}"
        };
        _db.Invoices.Add(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RateAsync(int episodeId, RateRequest req, CancellationToken ct)
    {
        var family = await RequireFamilyAsync(ct);
        var e = await _db.Episodes.Include(x => x.Elder).FirstOrDefaultAsync(x => x.Id == episodeId && x.Elder.FamilyId == family.Id, ct)
            ?? throw new AppException("Episode not found.", 404);
        if (e.StartDate.Date > IndiaTime.Today.AddDays(-7))
            throw new AppException("Rate after 7 days of care.");
        if (req.Stars is < 1 or > 5) throw new AppException("Stars must be 1 to 5.");
        _db.Ratings.Add(new Rating { EpisodeId = episodeId, Stars = req.Stars, Comment = req.Comment });
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetGstAsync(GstSettingsRequest req, CancellationToken ct)
    {
        if (_current.Role == AppRoles.AgencyAdmin)
        {
            var agency = await RequireAgencyAsync(ct);
            agency.GstEnabled = req.GstEnabled; agency.Gstin = req.Gstin; agency.LegalName = req.LegalName;
            agency.Address = req.Address; agency.CgstPercent = req.CgstPercent; agency.SgstPercent = req.SgstPercent; agency.IgstPercent = req.IgstPercent;
        }
        else
        {
            var cg = await RequireCaregiverAsync(ct);
            cg.GstEnabled = req.GstEnabled; cg.Gstin = req.Gstin; cg.LegalName = req.LegalName;
            cg.Address = req.Address; cg.CgstPercent = req.CgstPercent; cg.SgstPercent = req.SgstPercent; cg.IgstPercent = req.IgstPercent;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<GstSettingsRequest> GetGstAsync(CancellationToken ct)
    {
        if (_current.Role == AppRoles.AgencyAdmin)
        {
            var agency = await RequireAgencyAsync(ct);
            return new GstSettingsRequest(agency.GstEnabled, agency.Gstin, agency.LegalName, agency.Address, agency.CgstPercent, agency.SgstPercent, agency.IgstPercent);
        }
        var cg = await RequireCaregiverAsync(ct);
        return new GstSettingsRequest(cg.GstEnabled, cg.Gstin, cg.LegalName, cg.Address, cg.CgstPercent, cg.SgstPercent, cg.IgstPercent);
    }

    public async Task OptOutWhatsAppAsync(bool optOut, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(RequireUser()) ?? throw new AppException("User not found.", 404);
        user.WhatsAppOptOut = optOut;
        await _users.UpdateAsync(user);
    }

    public async Task SubscribePushAsync(PushSubscribeRequest req, CancellationToken ct)
    {
        var userId = RequireUser();
        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(p => p.UserId == userId && p.Endpoint == req.Endpoint, ct);
        if (existing is null)
            _db.PushSubscriptions.Add(new PushSubscription { UserId = userId, Endpoint = req.Endpoint, P256dh = req.P256dh, Auth = req.Auth });
        else
        {
            existing.P256dh = req.P256dh; existing.Auth = req.Auth;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetKycAsync(int caregiverId, KycDecisionRequest req, CancellationToken ct)
    {
        RequireAdmin();
        var cg = await _db.Caregivers.FirstOrDefaultAsync(c => c.Id == caregiverId, ct) ?? throw new AppException("Not found.", 404);
        cg.KycStatus = req.Status;
        cg.KycReason = req.Reason;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<object>> KycQueueAsync(CancellationToken ct)
    {
        RequireAdmin();
        return await _db.Caregivers.Include(c => c.User)
            .Where(c => c.KycStatus == KycStatus.Pending)
            .Select(c => (object)new { c.Id, c.User.FullName, c.Phone, c.City, c.Kind })
            .ToListAsync(ct);
    }

    public async Task DisableAsync(string userId, bool active, CancellationToken ct)
    {
        RequireAdmin();
        var user = await _users.FindByIdAsync(userId) ?? throw new AppException("User not found.", 404);
        user.IsActive = active;
        await _users.UpdateAsync(user);
    }

    public async Task<List<AuditDto>> AuditListAsync(CancellationToken ct)
    {
        RequireAdmin();
        return await _db.AuditEvents.OrderByDescending(a => a.At).Take(200)
            .Select(a => new AuditDto(a.Id, a.UserId, a.ElderId, a.Action, a.At)).ToListAsync(ct);
    }

    public async Task<object> ExportAsync(CancellationToken ct)
    {
        var family = await RequireFamilyAsync(ct);
        var elders = await _db.Elders.Where(e => e.FamilyId == family.Id).ToListAsync(ct);
        var ids = elders.Select(e => e.Id).ToList();
        var episodes = await _db.Episodes.Where(e => ids.Contains(e.ElderId)).ToListAsync(ct);
        var eids = episodes.Select(e => e.Id).ToList();
        var logs = await _db.CareLogs.Where(l => eids.Contains(l.EpisodeId)).ToListAsync(ct);
        return new { elders, episodes, logs };
    }

    public async Task AddBranchAsync(BranchRequest req, CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        _db.Branches.Add(new Branch { AgencyId = agency.Id, Name = req.Name.Trim(), City = req.City.Trim() });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<BranchDto>> BranchesAsync(CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        return await _db.Branches.Where(b => b.AgencyId == agency.Id).Select(b => new BranchDto(b.Id, b.Name, b.City)).ToListAsync(ct);
    }

    public async Task<List<StaffDto>> StaffAsync(CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        return await _db.Caregivers.Include(c => c.User).Where(c => c.AgencyId == agency.Id)
            .Select(c => new StaffDto(c.Id, c.User.FullName, c.Kind, c.KycStatus)).ToListAsync(ct);
    }

    public async Task<OccupancyDto> OccupancyAsync(CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        var staffIds = await _db.Caregivers.Where(c => c.AgencyId == agency.Id).Select(c => c.Id).ToListAsync(ct);
        var shifts = await _db.Shifts.Where(s => staffIds.Contains(s.CaregiverId) && s.Status == ShiftStatus.Planned).ToListAsync(ct);
        var hours = (int)shifts.Sum(s => (s.SlotEnd - s.SlotStart).TotalHours);
        var billed = await _db.Invoices.Where(i => i.AgencyId == agency.Id && !i.IsCommission).SumAsync(i => (int?)i.HoursOrDays, ct) ?? 0;
        return new OccupancyDto(hours, billed);
    }

    public async Task AddConsumableAsync(ConsumableRequest req, CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        _db.ConsumableIssues.Add(new ConsumableIssue { AgencyId = agency.Id, ElderId = req.ElderId, Item = req.Item.Trim(), Quantity = req.Quantity });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ConsumableDto>> ConsumablesAsync(CancellationToken ct)
    {
        var agency = await RequireAgencyAsync(ct);
        return await _db.ConsumableIssues.Where(c => c.AgencyId == agency.Id).OrderByDescending(c => c.At)
            .Select(c => new ConsumableDto(c.Id, c.ElderId, c.Item, c.Quantity, c.At)).Take(50).ToListAsync(ct);
    }

    public async Task<PhotoDto> UploadEpisodePhotoAsync(int episodeId, Stream content, string contentType, string kind, CancellationToken ct)
    {
        var e = await EnsureEpisodeAccessAsync(episodeId, ct);
        if (!string.Equals(kind, "Wound", StringComparison.OrdinalIgnoreCase) && !string.Equals(kind, "Id", StringComparison.OrdinalIgnoreCase))
            throw new AppException("Kind must be Wound or Id.");
        if (!e.Elder.ConsentPhotos)
            throw new AppException("The family has not consented to photos.");
        if (contentType is not ("image/jpeg" or "image/jpg" or "image/png" or "image/webp"))
            throw new AppException("Use a JPEG, PNG, or WebP photo.");
        var key = await _photos.SaveAsync(content, contentType, ct);
        var row = new CarePhoto { EpisodeId = episodeId, Kind = kind, BlobKey = key, ContentType = contentType };
        _db.CarePhotos.Add(row);
        await _db.SaveChangesAsync(ct);
        return new PhotoDto(row.Id, row.Kind, $"/api/v1/photos/{row.Id}", row.At);
    }

    public async Task<List<PhotoDto>> PhotosAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        return await _db.CarePhotos.Where(p => p.EpisodeId == episodeId).OrderByDescending(p => p.At)
            .Select(p => new PhotoDto(p.Id, p.Kind, "/api/v1/photos/" + p.Id, p.At)).ToListAsync(ct);
    }

    public async Task<(Stream Content, string ContentType)> OpenPhotoAsync(int id, CancellationToken ct)
    {
        var row = await _db.CarePhotos.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new AppException("Photo not found.", 404);
        await EnsureEpisodeAccessAsync(row.EpisodeId, ct);
        var file = await _photos.OpenAsync(row.BlobKey, ct) ?? throw new AppException("Photo file is missing.", 404);
        return file;
    }

    public async Task UploadIdPhotoAsync(Stream content, string contentType, CancellationToken ct)
    {
        if (contentType is not ("image/jpeg" or "image/jpg" or "image/png" or "image/webp"))
            throw new AppException("Use a JPEG, PNG, or WebP photo.");
        var cg = await RequireCaregiverAsync(ct);
        var key = await _photos.SaveAsync(content, contentType, ct);
        _db.KycDocuments.Add(new KycDocument { CaregiverId = cg.Id, Kind = "Id", Label = "ID photo", BlobKey = key, ContentType = contentType });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<RatingDto>> RatingsAsync(int episodeId, CancellationToken ct)
    {
        await EnsureEpisodeAccessAsync(episodeId, ct);
        return await _db.Ratings.Where(r => r.EpisodeId == episodeId)
            .Select(r => new RatingDto(r.Stars, r.Comment)).ToListAsync(ct);
    }

    public async Task HospitalStartAsync(HospitalPacketRequest req, CancellationToken ct)
    {
        var h = await RequireHospitalAsync(ct);
        var elder = await _db.Elders.FirstOrDefaultAsync(e => e.Id == req.ElderId, ct) ?? throw new AppException("Elder not found.", 404);
        var episode = await EnsureEpisodeAsync(elder.Id, req.AgencyId, ct);
        episode.HospitalOrgId = h.HospitalOrgId;
        if (episode.HospitalPacket is null)
            _db.HospitalPackets.Add(new HospitalPacket { EpisodeId = episode.Id, Diagnosis = req.Diagnosis, Meds = req.Meds, FollowUpOn = req.FollowUpOn });
        else
        {
            episode.HospitalPacket.Diagnosis = req.Diagnosis;
            episode.HospitalPacket.Meds = req.Meds;
            episode.HospitalPacket.FollowUpOn = req.FollowUpOn;
        }
        if (req.CaregiverId is int cid)
            await AssignAndShiftAsync(episode.Id, cid, DateTime.UtcNow, DateTime.UtcNow.AddHours(12), ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<VisitDto>> HospitalVisitsAsync(CancellationToken ct)
    {
        var h = await RequireHospitalAsync(ct);
        return await _db.Visits.Include(v => v.Shift).ThenInclude(s => s.Episode)
            .Where(v => v.Shift.Episode.HospitalOrgId == h.HospitalOrgId)
            .OrderByDescending(v => v.Id).Take(100)
            .Select(v => new VisitDto(v.Id, v.ShiftId, v.Status, v.CheckInAt, v.CheckOutAt, v.Note))
            .ToListAsync(ct);
    }

    private async Task<CareEpisode> EnsureEpisodeAsync(int elderId, int? agencyId, CancellationToken ct)
    {
        var existing = await _db.Episodes.Include(e => e.HospitalPacket).FirstOrDefaultAsync(e => e.ElderId == elderId && e.Active, ct);
        if (existing is not null)
        {
            if (agencyId is int aid) existing.AgencyId = aid;
            return existing;
        }
        var e = new CareEpisode { ElderId = elderId, AgencyId = agencyId, StartDate = IndiaTime.Today, PlanTemplate = "general" };
        _db.Episodes.Add(e);
        await _db.SaveChangesAsync(ct);
        return e;
    }

    private async Task AssignAndShiftAsync(int episodeId, int caregiverId, DateTime start, DateTime end, CancellationToken ct)
    {
        _db.Assignments.Add(new Assignment { EpisodeId = episodeId, CaregiverId = caregiverId });
        var shift = new Shift { EpisodeId = episodeId, CaregiverId = caregiverId, SlotStart = start, SlotEnd = end };
        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(ct);
        _db.Visits.Add(new Visit { ShiftId = shift.Id, Status = VisitStatus.Scheduled });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool> OverlapsAsync(int caregiverId, DateTime start, DateTime end, CancellationToken ct)
    {
        var busy = await _db.Shifts.AnyAsync(s => s.CaregiverId == caregiverId && s.Status == ShiftStatus.Planned && s.SlotStart < end && start < s.SlotEnd, ct);
        var blocked = await _db.CalendarBlocks.AnyAsync(b => b.CaregiverId == caregiverId && b.StartAt < end && start < b.EndAt, ct);
        return busy || blocked;
    }

    private async Task<CareEpisode> EnsureEpisodeAccessAsync(int episodeId, CancellationToken ct)
    {
        var e = await _db.Episodes.Include(x => x.Elder).Include(x => x.HospitalPacket)
            .FirstOrDefaultAsync(x => x.Id == episodeId, ct) ?? throw new AppException("Episode not found.", 404);
        if (_current.Role == AppRoles.Family)
        {
            var family = await RequireFamilyAsync(ct);
            if (e.Elder.FamilyId != family.Id) throw new AppException("Episode not found.", 404);
        }
        else if (_current.Role == AppRoles.Caregiver)
        {
            var cg = await RequireCaregiverAsync(ct);
            var ok = await _db.Assignments.AnyAsync(a => a.EpisodeId == e.Id && a.CaregiverId == cg.Id, ct)
                || (cg.AgencyId is int aid && e.AgencyId == aid);
            if (!ok && _current.Role != AppRoles.AgencyAdmin) throw new AppException("Episode not found.", 404);
        }
        else if (_current.Role == AppRoles.HospitalCoordinator)
        {
            var h = await RequireHospitalAsync(ct);
            if (e.HospitalOrgId != h.HospitalOrgId) throw new AppException("Episode not found.", 404);
        }
        else if (_current.Role != AppRoles.PlatformAdmin && _current.Role != AppRoles.AgencyAdmin)
            throw new AppException("Episode not found.", 404);
        return e;
    }

    private static ElderDto MapElder(Elder e) =>
        new(e.Id, e.FullName, e.Age, e.Address, e.City, e.Conditions, e.Allergies, e.EmergencyContact, e.ConsentHealth, e.ConsentPhotos);

    private static CareRequestDto MapRequest(CareRequest r, string elderName) =>
        new(r.Id, r.ElderId, elderName, r.CaregiverId, r.AgencyId, r.Mode, r.StartAt, r.EndAt, r.Notes, r.Status, r.InstantBook);

    private static InvoiceDto MapInvoice(Invoice i) =>
        new(i.Id, i.Number, i.Total, i.Tax, i.PaymentStatus, i.PaymentMode, i.GstEnabledSnapshot, i.IssuedAt, i.IsCommission);
}
