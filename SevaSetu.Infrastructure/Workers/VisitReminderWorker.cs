using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SevaSetu.Infrastructure.Persistence;

namespace SevaSetu.Infrastructure.Workers;

public class VisitReminderWorker(IServiceProvider services, IConfiguration config, ILogger<VisitReminderWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SevaSetu reminder tick failed; booking is unaffected.");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var from = now.AddMinutes(50);
        var to = now.AddMinutes(70);
        var shifts = await db.Shifts.Include(s => s.Episode).ThenInclude(e => e.Elder).ThenInclude(e => e.Family).ThenInclude(f => f.User)
            .Where(s => s.Status == ShiftStatus.Planned && s.ReminderSentAt == null && s.SlotStart >= from && s.SlotStart <= to)
            .ToListAsync(ct);
        var token = config["WhatsApp:AccessToken"];
        var phoneId = config["WhatsApp:PhoneNumberId"];
        var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        foreach (var shift in shifts)
        {
            var familyUser = shift.Episode.Elder.Family.User;
            shift.ReminderSentAt = DateTime.UtcNow;
            if (familyUser.WhatsAppOptOut)
            {
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Status = "opt-out" });
                continue;
            }
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId))
            {
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Status = "skipped", Detail = "WhatsApp Cloud API is not configured." });
                continue;
            }
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v21.0/{phoneId}/messages");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(new
                {
                    messaging_product = "whatsapp",
                    to = familyUser.PhoneNumber,
                    type = "template",
                    template = new { name = config["WhatsApp:VisitTemplate"] ?? "sevasetu_visit_reminder", language = new { code = "en" } }
                });
                var res = await http.SendAsync(req, ct);
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Status = res.IsSuccessStatusCode ? "sent" : "failed", Detail = res.StatusCode.ToString() });
            }
            catch (Exception ex)
            {
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Status = "failed", Detail = ex.Message });
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
