using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SevaSetu.Application.Common;
using SevaSetu.Application.Dtos;

namespace SevaSetu.Application.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(UserManager<ApplicationUser> users, IAppDbContext db, IConfiguration config)
    {
        _users = users;
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterFamilyAsync(RegisterFamilyRequest req, CancellationToken ct)
    {
        var (user, phone) = await CreateUserAsync(req.FullName, req.Phone, req.Password, AppRoles.Family, ct);
        _db.Families.Add(new FamilyProfile { UserId = user.Id, Phone = phone, City = string.IsNullOrWhiteSpace(req.City) ? "Delhi NCR" : req.City.Trim() });
        await _db.SaveChangesAsync(ct);
        return await IssueAsync(user);
    }

    public async Task<AuthResponse> RegisterCaregiverAsync(RegisterCaregiverRequest req, CancellationToken ct)
    {
        var (user, phone) = await CreateUserAsync(req.FullName, req.Phone, req.Password, AppRoles.Caregiver, ct);
        _db.Caregivers.Add(new CaregiverProfile
        {
            UserId = user.Id,
            Phone = phone,
            City = req.City.Trim(),
            Kind = req.Kind,
            Years = req.Years,
            Languages = string.IsNullOrWhiteSpace(req.Languages) ? "Hindi, English" : req.Languages.Trim(),
            Gender = req.Gender,
            HourlyRate = req.HourlyRate <= 0 ? 300 : req.HourlyRate,
            LiveInOk = req.LiveInOk,
            KycStatus = KycStatus.Pending
        });
        await _db.SaveChangesAsync(ct);
        return await IssueAsync(user);
    }

    public async Task<AuthResponse> RegisterAgencyAsync(RegisterAgencyRequest req, CancellationToken ct)
    {
        var (user, phone) = await CreateUserAsync(req.FullName, req.Phone, req.Password, AppRoles.AgencyAdmin, ct);
        var agency = new Agency { Name = req.AgencyName.Trim(), City = req.City.Trim() };
        _db.Agencies.Add(agency);
        await _db.SaveChangesAsync(ct);
        _db.Caregivers.Add(new CaregiverProfile
        {
            UserId = user.Id,
            Phone = phone,
            City = req.City.Trim(),
            Kind = CaregiverKind.Nurse,
            AgencyId = agency.Id,
            KycStatus = KycStatus.Passed,
            HourlyRate = 0
        });
        await _db.SaveChangesAsync(ct);
        return await IssueAsync(user);
    }

    public async Task<AuthResponse> RegisterHospitalAsync(RegisterHospitalRequest req, CancellationToken ct)
    {
        var (user, phone) = await CreateUserAsync(req.FullName, req.Phone, req.Password, AppRoles.HospitalCoordinator, ct);
        var hospital = new HospitalOrg { Name = req.HospitalName.Trim(), City = req.City.Trim() };
        _db.Hospitals.Add(hospital);
        await _db.SaveChangesAsync(ct);
        _db.HospitalCoordinators.Add(new HospitalCoordinator { UserId = user.Id, Phone = phone, HospitalOrgId = hospital.Id });
        await _db.SaveChangesAsync(ct);
        return await IssueAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(AuthRequest req)
    {
        var identifier = req.Email?.Trim() ?? "";
        ApplicationUser? user;
        if (identifier.Contains('@'))
            user = await _users.FindByEmailAsync(identifier);
        else
        {
            var phone = NormalizePhone(identifier);
            user = await _users.FindByNameAsync(phone)
                ?? await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }
        if (user is null || !await _users.CheckPasswordAsync(user, req.Password))
            throw new AppException("Invalid credentials.", 401);
        if (!user.IsActive)
            throw new AppException("Account is disabled.", 403);
        return await IssueAsync(user);
    }

    public Task<AuthResponse> LoginByPhoneAsync(LoginByPhoneRequest req)
        => LoginAsync(new AuthRequest(req.Phone, req.Password));

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var stored = await _db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.Revoked, ct)
            ?? throw new AppException("Invalid refresh token.", 401);
        if (stored.ExpiresAt < DateTime.UtcNow)
            throw new AppException("Refresh token expired.", 401);
        stored.Revoked = true;
        await _db.SaveChangesAsync(ct);
        return await IssueAsync(stored.User);
    }

    public async Task LogoutAsync(string? refreshToken, string? userId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var tokens = await _db.RefreshTokens.Where(r => r.UserId == userId && !r.Revoked).ToListAsync(ct);
            foreach (var t in tokens) t.Revoked = true;
            await _db.SaveChangesAsync(ct);
            return;
        }
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken && !r.Revoked, ct);
        if (stored is null) return;
        stored.Revoked = true;
        await _db.SaveChangesAsync(ct);
    }

    public static string NormalizePhone(string phone) => Regex.Replace(phone ?? "", @"[^\d+]", "");
    public static string PhoneEmail(string phone) => $"{NormalizePhone(phone)}@sevasetu.local";

    private async Task<(ApplicationUser user, string phone)> CreateUserAsync(string fullName, string phoneRaw, string password, string role, CancellationToken ct)
    {
        var phone = NormalizePhone(phoneRaw);
        if (phone.Length < 8) throw new AppException("Valid mobile number is required.");
        if (await _users.Users.AnyAsync(u => u.PhoneNumber == phone, ct))
            throw new AppException("This mobile number is already registered.");
        var email = phone.Contains('@') ? phone : PhoneEmail(phone);
        var user = new ApplicationUser
        {
            UserName = phone,
            Email = email,
            FullName = fullName.Trim(),
            PhoneNumber = phone,
            EmailConfirmed = true
        };
        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new AppException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await _users.AddToRoleAsync(user, role);
        return (user, phone);
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRoles.Family;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role)
        };
        var minutes = int.Parse(_config["Jwt:AccessMinutes"] ?? "60");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);
        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh,
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        });
        await _db.SaveChangesAsync();
        return new AuthResponse(access, refresh, role, user.FullName, user.Email ?? user.PhoneNumber ?? "");
    }
}
