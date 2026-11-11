using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SevaSetu.Application.Common;

namespace SevaSetu.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.Family, AppRoles.Caregiver, AppRoles.AgencyAdmin, AppRoles.HospitalCoordinator, AppRoles.PlatformAdmin, AppRoles.Elder })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        async Task<ApplicationUser> EnsureUser(string email, string userName, string fullName, string phone, string password, string role)
        {
            var user = await users.FindByEmailAsync(email) ?? await users.FindByNameAsync(userName);
            if (user is null)
            {
                user = new ApplicationUser { UserName = userName, Email = email, FullName = fullName, PhoneNumber = phone, EmailConfirmed = true };
                var result = await users.CreateAsync(user, password);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
                await users.AddToRoleAsync(user, role);
            }
            else if (!await users.IsInRoleAsync(user, role))
                await users.AddToRoleAsync(user, role);
            return user;
        }

        var admin = await EnsureUser("admin@sevasetu.local", "admin@sevasetu.local", "SevaSetu Admin", "9000000099", "Admin@12345", AppRoles.PlatformAdmin);
        var familyUser = await EnsureUser("family@sevasetu.local", "9811100001", "Kavita Mehra", "9811100001", "Family@12345", AppRoles.Family);
        var nurseUser = await EnsureUser("nurse@sevasetu.local", "9811100002", "Anita Nurse", "9811100002", "Caregiver@12345", AppRoles.Caregiver);
        var att1User = await EnsureUser("attendant@sevasetu.local", "9811100003", "Ramesh Attendant", "9811100003", "Caregiver@12345", AppRoles.Caregiver);
        var att2User = await EnsureUser("attendant2@sevasetu.local", "9811100004", "Sita Attendant", "9811100004", "Caregiver@12345", AppRoles.Caregiver);
        var agencyUser = await EnsureUser("agency@sevasetu.local", "agency@sevasetu.local", "Seva Agency Admin", "9811100005", "Agency@12345", AppRoles.AgencyAdmin);
        var hospitalUser = await EnsureUser("hospital@sevasetu.local", "hospital@sevasetu.local", "Dr. Coordinator", "9811100006", "Hospital@12345", AppRoles.HospitalCoordinator);

        if (!await db.Families.AnyAsync(f => f.UserId == familyUser.Id))
            db.Families.Add(new FamilyProfile { UserId = familyUser.Id, Phone = "9811100001", City = "Delhi NCR" });
        if (!await db.Agencies.AnyAsync())
            db.Agencies.Add(new Agency { Name = "Seva Home Agency", City = "Delhi NCR", HourlyRate = 250, LiveInDayRate = 1800 });
        if (!await db.Hospitals.AnyAsync())
            db.Hospitals.Add(new HospitalOrg { Name = "NCR General Hospital", City = "Delhi NCR" });
        await db.SaveChangesAsync();

        var family = await db.Families.FirstAsync(f => f.UserId == familyUser.Id);
        var agency = await db.Agencies.FirstAsync();
        var hospital = await db.Hospitals.FirstAsync();

        if (!await db.Branches.AnyAsync())
            db.Branches.Add(new Branch { AgencyId = agency.Id, Name = "South Delhi", City = "Delhi NCR" });

        async Task EnsureCaregiver(ApplicationUser user, CaregiverKind kind, int? agencyId, KycStatus kyc, decimal hourly)
        {
            if (await db.Caregivers.AnyAsync(c => c.UserId == user.Id)) return;
            db.Caregivers.Add(new CaregiverProfile
            {
                UserId = user.Id, Phone = user.PhoneNumber ?? "", City = "Delhi NCR", Kind = kind,
                AgencyId = agencyId, KycStatus = kyc, HourlyRate = hourly, LiveInDayRate = hourly * 6, Years = 5,
                Languages = "Hindi, English", LiveInOk = true
            });
        }

        await EnsureCaregiver(nurseUser, CaregiverKind.Nurse, null, KycStatus.Passed, 400);
        await EnsureCaregiver(att1User, CaregiverKind.Attendant, agency.Id, KycStatus.Passed, 220);
        await EnsureCaregiver(att2User, CaregiverKind.Attendant, agency.Id, KycStatus.Passed, 220);
        await EnsureCaregiver(agencyUser, CaregiverKind.Nurse, agency.Id, KycStatus.Passed, 0);
        if (!await db.HospitalCoordinators.AnyAsync(h => h.UserId == hospitalUser.Id))
            db.HospitalCoordinators.Add(new HospitalCoordinator { UserId = hospitalUser.Id, HospitalOrgId = hospital.Id, Phone = "9811100006" });
        await db.SaveChangesAsync();

        if (!await db.Elders.AnyAsync())
        {
            db.Elders.Add(new Elder
            {
                FamilyId = family.Id, FullName = "Om Prakash Mehra", Age = 78,
                Address = "12 Green Park", City = "Delhi NCR", Conditions = "Hypertension, mild dementia",
                EmergencyContact = "9811100001", ConsentHealth = true
            });
            await db.SaveChangesAsync();
        }

        var elder = await db.Elders.FirstAsync();
        var nurse = await db.Caregivers.FirstAsync(c => c.UserId == nurseUser.Id);
        if (!await db.Episodes.AnyAsync())
        {
            var start = IndiaTime.Today.AddDays(-10);
            var episode = new CareEpisode { ElderId = elder.Id, StartDate = start, PlanTemplate = "dementia", PlanNotes = "Assist meals and evening walk.", HospitalOrgId = hospital.Id };
            db.Episodes.Add(episode);
            await db.SaveChangesAsync();
            db.HospitalPackets.Add(new HospitalPacket { EpisodeId = episode.Id, Diagnosis = "Post-discharge observation", Meds = "Amlodipine 5mg morning", FollowUpOn = start.AddDays(14) });
            db.Assignments.Add(new Assignment { EpisodeId = episode.Id, CaregiverId = nurse.Id, StartedAt = start });
            var shiftStart = DateTime.UtcNow.AddHours(2);
            var shift = new Shift { EpisodeId = episode.Id, CaregiverId = nurse.Id, SlotStart = shiftStart, SlotEnd = shiftStart.AddHours(8) };
            db.Shifts.Add(shift);
            await db.SaveChangesAsync();
            db.Visits.Add(new Visit { ShiftId = shift.Id, Status = VisitStatus.Scheduled });
            db.CareLogs.Add(new CareLog { EpisodeId = episode.Id, LogDate = IndiaTime.Today.AddDays(-1), ComfortScore = 4, Meals = "Lunch taken", Mobility = "Walked in corridor" });
            db.VitalLogs.Add(new VitalLog { EpisodeId = episode.Id, LogDate = IndiaTime.Today.AddDays(-1), Systolic = 130, Diastolic = 80, Pulse = 72 });
            db.Invoices.Add(new Invoice
            {
                EpisodeId = episode.Id, CaregiverId = nurse.Id, Number = "SS-SEED-0001",
                PeriodStart = start, PeriodEnd = start.AddDays(7), HoursOrDays = 8, RateSnapshot = 400,
                SubTotal = 3200, Tax = 0, Total = 3200, PaymentStatus = PaymentStatus.Unpaid
            });
            await db.SaveChangesAsync();
        }
    }
}
