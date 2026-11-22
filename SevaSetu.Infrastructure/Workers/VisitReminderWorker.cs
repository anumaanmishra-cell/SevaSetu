using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SevaSetu.Infrastructure.Persistence;
using WebPush;

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
        var template = config["WhatsApp:VisitTemplate"] ?? "sevasetu_visit_reminder";
        var vapidPublic = config["Vapid:PublicKey"];
        var vapidPrivate = config["Vapid:PrivateKey"];
        var vapidSubject = config["Vapid:Subject"] ?? "mailto:admin@sevasetu.local";
        var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        foreach (var shift in shifts)
        {
            var familyUser = shift.Episode.Elder.Family.User;
            shift.ReminderSentAt = DateTime.UtcNow;
            var when = shift.SlotStart.ToString("dd MMM HH:mm");
            var elderName = shift.Episode.Elder.FullName;
            if (familyUser.WhatsAppOptOut)
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "whatsapp", Status = "opt-out" });
            else if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId))
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "whatsapp", Status = "skipped", Detail = "WhatsApp Cloud API is not configured." });
            else
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v21.0/{phoneId}/messages");
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    req.Content = JsonContent.Create(new
                    {
                        messaging_product = "whatsapp",
                        to = familyUser.PhoneNumber,
                        type = "template",
                        template = new
                        {
                            name = template,
                            language = new { code = "en" },
                            components = new[]
                            {
                                new
                                {
                                    type = "body",
                                    parameters = new[]
                                    {
                                        new { type = "text", text = elderName },
                                        new { type = "text", text = when }
                                    }
                                }
                            }
                        }
                    });
                    var res = await http.SendAsync(req, ct);
                    db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "whatsapp", Status = res.IsSuccessStatusCode ? "sent" : "failed", Detail = res.StatusCode.ToString() });
                }
                catch (Exception ex)
                {
                    db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "whatsapp", Status = "failed", Detail = ex.Message });
                }
            }

            if (string.IsNullOrWhiteSpace(vapidPublic) || string.IsNullOrWhiteSpace(vapidPrivate))
                db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "webpush", Status = "skipped", Detail = "VAPID is not configured on App Service." });
            else
            {
                var subs = await db.PushSubscriptions.Where(p => p.UserId == familyUser.Id).ToListAsync(ct);
                if (subs.Count == 0)
                    db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "webpush", Status = "skipped", Detail = "No browser subscription." });
                else
                {
                    var client = new WebPushClient();
                    var vapid = new VapidDetails(vapidSubject, vapidPublic, vapidPrivate);
                    var payload = System.Text.Json.JsonSerializer.Serialize(new { title = "SevaSetu visit", body = $"{elderName} · {when}" });
                    foreach (var sub in subs)
                    {
                        try
                        {
                            await client.SendNotificationAsync(new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth), payload, vapid);
                            db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "webpush", Status = "sent" });
                        }
                        catch (Exception ex)
                        {
                            db.ReminderLogs.Add(new ReminderLog { ShiftId = shift.Id, Channel = "webpush", Status = "failed", Detail = ex.Message });
                        }
                    }
                }
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
