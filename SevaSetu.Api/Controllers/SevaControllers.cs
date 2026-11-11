using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SevaSetu.Application.Dtos;
using SevaSetu.Application.Services;
using SevaSetu.Domain;

namespace SevaSetu.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class SevaControllers(CareService care) : ControllerBase
{
    [HttpGet("directory/caregivers")]
    [Authorize]
    public async Task<ActionResult<List<DirectoryCaregiverDto>>> Caregivers([FromQuery] string? city, [FromQuery] CaregiverKind? kind, CancellationToken ct)
        => Ok(await care.SearchCaregiversAsync(city, kind, ct));

    [HttpGet("directory/agencies")]
    [Authorize]
    public async Task<ActionResult<List<DirectoryAgencyDto>>> Agencies([FromQuery] string? city, CancellationToken ct)
        => Ok(await care.SearchAgenciesAsync(city, ct));

    [HttpGet("family/elders")]
    [Authorize(Roles = AppRoles.Family)]
    public async Task<ActionResult<List<ElderDto>>> Elders(CancellationToken ct) => Ok(await care.MyEldersAsync(ct));

    [HttpPost("family/elders")]
    [Authorize(Roles = AppRoles.Family)]
    public async Task<ActionResult<ElderDto>> AddElder(ElderRequest req, CancellationToken ct) => Ok(await care.AddElderAsync(req, ct));

    [HttpPost("family/requests")]
    [Authorize(Roles = AppRoles.Family)]
    public async Task<ActionResult<CareRequestDto>> RequestCare(CreateCareRequest req, CancellationToken ct)
        => Ok(await care.CreateRequestAsync(req, ct));

    [HttpGet("requests")]
    [Authorize]
    public async Task<ActionResult<List<CareRequestDto>>> Inbox(CancellationToken ct) => Ok(await care.InboxAsync(ct));

    [HttpPost("requests/{id:int}/respond")]
    [Authorize(Roles = $"{AppRoles.Caregiver},{AppRoles.AgencyAdmin}")]
    public async Task<IActionResult> Respond(int id, [FromQuery] bool accept, [FromQuery] int? staffCaregiverId, CancellationToken ct)
    {
        await care.RespondRequestAsync(id, accept, staffCaregiverId, ct);
        return Ok(new { message = "Updated" });
    }

    [HttpGet("episodes")]
    [Authorize]
    public async Task<ActionResult<List<EpisodeDto>>> Episodes(CancellationToken ct) => Ok(await care.EpisodesAsync(ct));

    [HttpGet("episodes/{id:int}")]
    [Authorize]
    public async Task<ActionResult<EpisodeDto>> Episode(int id, CancellationToken ct) => Ok(await care.GetEpisodeAsync(id, ct));

    [HttpPut("episodes/{id:int}/plan")]
    [Authorize]
    public async Task<IActionResult> Plan(int id, UpdatePlanRequest req, CancellationToken ct)
    {
        await care.UpdatePlanAsync(id, req, ct);
        return Ok();
    }

    [HttpGet("shifts")]
    [Authorize]
    public async Task<ActionResult<List<ShiftDto>>> Shifts([FromQuery] int? episodeId, CancellationToken ct)
        => Ok(await care.ShiftsAsync(episodeId, ct));

    [HttpPost("shifts/{id:int}/check-in")]
    [Authorize(Roles = AppRoles.Caregiver)]
    public async Task<IActionResult> CheckIn(int id, CheckInRequest req, CancellationToken ct)
    {
        await care.CheckInAsync(id, req.Note, ct);
        return Ok();
    }

    [HttpPost("shifts/{id:int}/check-out")]
    [Authorize(Roles = AppRoles.Caregiver)]
    public async Task<IActionResult> CheckOut(int id, CheckInRequest req, CancellationToken ct)
    {
        await care.CheckOutAsync(id, req.Note, ct);
        return Ok();
    }

    [HttpPost("shifts/{id:int}/no-show")]
    [Authorize(Roles = $"{AppRoles.Family},{AppRoles.AgencyAdmin}")]
    public async Task<IActionResult> NoShow(int id, CancellationToken ct)
    {
        await care.MarkNoShowAsync(id, ct);
        return Ok();
    }

    [HttpPost("shifts/{id:int}/replace")]
    [Authorize(Roles = AppRoles.AgencyAdmin)]
    public async Task<IActionResult> Replace(int id, ReplaceShiftRequest req, CancellationToken ct)
    {
        await care.ReplaceShiftAsync(id, req.NewCaregiverId, ct);
        return Ok();
    }

    [HttpPost("calendar/block")]
    [Authorize(Roles = AppRoles.Caregiver)]
    public async Task<IActionResult> Block(BlockCalendarRequest req, CancellationToken ct)
    {
        await care.BlockCalendarAsync(req, ct);
        return Ok();
    }

    [HttpPost("episodes/{id:int}/log")]
    [Authorize]
    public async Task<IActionResult> Log(int id, SaveCareLogRequest req, CancellationToken ct)
    {
        await care.SaveLogAsync(id, req, ct);
        return Ok();
    }

    [HttpGet("episodes/{id:int}/logs")]
    [Authorize]
    public async Task<ActionResult<List<CareLogDto>>> Logs(int id, CancellationToken ct) => Ok(await care.LogsAsync(id, ct));

    [HttpPost("episodes/{id:int}/vitals")]
    [Authorize]
    public async Task<IActionResult> VitalsSave(int id, SaveVitalRequest req, CancellationToken ct)
    {
        await care.SaveVitalAsync(id, req, ct);
        return Ok();
    }

    [HttpGet("episodes/{id:int}/vitals")]
    [Authorize]
    public async Task<ActionResult<List<VitalDto>>> Vitals(int id, CancellationToken ct) => Ok(await care.VitalsAsync(id, ct));

    [HttpPost("episodes/{id:int}/adl")]
    [Authorize]
    public async Task<IActionResult> Adl(int id, AddAdlRequest req, CancellationToken ct)
    {
        await care.AddAdlAsync(id, req.Activity, ct);
        return Ok();
    }

    [HttpGet("episodes/{id:int}/adl")]
    [Authorize]
    public async Task<ActionResult<List<AdlDto>>> AdlList(int id, CancellationToken ct) => Ok(await care.AdlAsync(id, ct));

    [HttpPost("episodes/{id:int}/mar")]
    [Authorize]
    public async Task<IActionResult> Mar(int id, AddMarRequest req, CancellationToken ct)
    {
        await care.AddMarAsync(id, req, ct);
        return Ok();
    }

    [HttpGet("episodes/{id:int}/mar")]
    [Authorize]
    public async Task<ActionResult<List<MarDto>>> MarList(int id, CancellationToken ct) => Ok(await care.MarAsync(id, ct));

    [HttpPost("episodes/{id:int}/incidents")]
    [Authorize]
    public async Task<IActionResult> Incident(int id, AddIncidentRequest req, CancellationToken ct)
    {
        await care.AddIncidentAsync(id, req, ct);
        return Ok();
    }

    [HttpGet("episodes/{id:int}/incidents")]
    [Authorize]
    public async Task<ActionResult<List<IncidentDto>>> Incidents(int id, CancellationToken ct) => Ok(await care.IncidentsAsync(id, ct));

    [HttpPost("elders/{id:int}/sos")]
    [Authorize]
    public async Task<IActionResult> Sos(int id, SosDto req, CancellationToken ct)
    {
        await care.SosAsync(id, req.Ok, ct);
        return Ok();
    }

    [HttpPost("episodes/{id:int}/invoices")]
    [Authorize]
    public async Task<ActionResult<InvoiceDto>> Invoice(int id, IssueInvoiceRequest req, CancellationToken ct)
        => Ok(await care.IssueInvoiceAsync(id, req, ct));

    [HttpGet("invoices")]
    [Authorize]
    public async Task<ActionResult<List<InvoiceDto>>> Invoices([FromQuery] int? episodeId, CancellationToken ct)
        => Ok(await care.InvoicesAsync(episodeId, ct));

    [HttpGet("invoices/{id:int}/html")]
    [Authorize]
    public async Task<IActionResult> InvoiceHtml(int id, CancellationToken ct)
    {
        var html = await care.InvoiceHtmlAsync(id, ct);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPut("invoices/{id:int}/pay")]
    [Authorize]
    public async Task<IActionResult> Pay(int id, PayInvoiceRequest req, CancellationToken ct)
    {
        await care.PayInvoiceAsync(id, req.Mode, ct);
        return Ok();
    }

    [HttpPost("invoices/{id:int}/commission")]
    [Authorize(Roles = AppRoles.PlatformAdmin)]
    public async Task<IActionResult> Commission(int id, CancellationToken ct)
    {
        await care.IssueCommissionAsync(id, ct);
        return Ok();
    }

    [HttpPost("episodes/{id:int}/rate")]
    [Authorize(Roles = AppRoles.Family)]
    public async Task<IActionResult> Rate(int id, RateRequest req, CancellationToken ct)
    {
        await care.RateAsync(id, req, ct);
        return Ok();
    }

    [HttpPut("me/gst")]
    [Authorize]
    public async Task<IActionResult> Gst(GstSettingsRequest req, CancellationToken ct)
    {
        await care.SetGstAsync(req, ct);
        return Ok();
    }

    [HttpPut("me/whatsapp-opt-out")]
    [Authorize]
    public async Task<IActionResult> OptOut([FromQuery] bool optOut, CancellationToken ct)
    {
        await care.OptOutWhatsAppAsync(optOut, ct);
        return Ok();
    }

    [HttpPost("me/push")]
    [Authorize]
    public async Task<IActionResult> Push(PushSubscribeRequest req, CancellationToken ct)
    {
        await care.SubscribePushAsync(req, ct);
        return Ok();
    }

    [HttpGet("family/export")]
    [Authorize(Roles = AppRoles.Family)]
    public async Task<IActionResult> Export(CancellationToken ct) => Ok(await care.ExportAsync(ct));

    [HttpGet("admin/kyc")]
    [Authorize(Roles = AppRoles.PlatformAdmin)]
    public async Task<IActionResult> KycQueue(CancellationToken ct) => Ok(await care.KycQueueAsync(ct));

    [HttpPost("admin/kyc/{caregiverId:int}")]
    [Authorize(Roles = AppRoles.PlatformAdmin)]
    public async Task<IActionResult> Kyc(int caregiverId, KycDecisionRequest req, CancellationToken ct)
    {
        await care.SetKycAsync(caregiverId, req, ct);
        return Ok();
    }

    [HttpPost("admin/users")]
    [Authorize(Roles = AppRoles.PlatformAdmin)]
    public async Task<IActionResult> Disable(DisableUserRequest req, CancellationToken ct)
    {
        await care.DisableAsync(req.UserId, req.IsActive, ct);
        return Ok();
    }

    [HttpGet("admin/audit")]
    [Authorize(Roles = AppRoles.PlatformAdmin)]
    public async Task<IActionResult> Audit(CancellationToken ct) => Ok(await care.AuditListAsync(ct));

    [HttpGet("agency/branches")]
    [Authorize(Roles = AppRoles.AgencyAdmin)]
    public async Task<ActionResult<List<BranchDto>>> Branches(CancellationToken ct) => Ok(await care.BranchesAsync(ct));

    [HttpPost("agency/branches")]
    [Authorize(Roles = AppRoles.AgencyAdmin)]
    public async Task<IActionResult> AddBranch(BranchRequest req, CancellationToken ct)
    {
        await care.AddBranchAsync(req, ct);
        return Ok();
    }

    [HttpGet("agency/staff")]
    [Authorize(Roles = AppRoles.AgencyAdmin)]
    public async Task<ActionResult<List<StaffDto>>> Staff(CancellationToken ct) => Ok(await care.StaffAsync(ct));

    [HttpGet("agency/occupancy")]
    [Authorize(Roles = AppRoles.AgencyAdmin)]
    public async Task<ActionResult<OccupancyDto>> Occupancy(CancellationToken ct) => Ok(await care.OccupancyAsync(ct));

    [HttpPost("agency/consumables")]
    [Authorize(Roles = AppRoles.AgencyAdmin)]
    public async Task<IActionResult> Consumable(ConsumableRequest req, CancellationToken ct)
    {
        await care.AddConsumableAsync(req, ct);
        return Ok();
    }

    [HttpPost("hospital/packet")]
    [Authorize(Roles = AppRoles.HospitalCoordinator)]
    public async Task<IActionResult> Packet(HospitalPacketRequest req, CancellationToken ct)
    {
        await care.HospitalStartAsync(req, ct);
        return Ok();
    }

    [HttpGet("hospital/visits")]
    [Authorize(Roles = AppRoles.HospitalCoordinator)]
    public async Task<ActionResult<List<VisitDto>>> HospitalVisits(CancellationToken ct)
        => Ok(await care.HospitalVisitsAsync(ct));
}
