using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SevaSetu.Application.Dtos;
using SevaSetu.Application.Services;

namespace SevaSetu.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register-family")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Family(RegisterFamilyRequest req, CancellationToken ct)
        => Ok(await auth.RegisterFamilyAsync(req, ct));

    [HttpPost("register-caregiver")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Caregiver(RegisterCaregiverRequest req, CancellationToken ct)
        => Ok(await auth.RegisterCaregiverAsync(req, ct));

    [HttpPost("register-agency")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Agency(RegisterAgencyRequest req, CancellationToken ct)
        => Ok(await auth.RegisterAgencyAsync(req, ct));

    [HttpPost("register-hospital")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Hospital(RegisterHospitalRequest req, CancellationToken ct)
        => Ok(await auth.RegisterHospitalAsync(req, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(AuthRequest req) => Ok(await auth.LoginAsync(req));

    [HttpPost("login-phone")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginPhone(LoginByPhoneRequest req) => Ok(await auth.LoginByPhoneAsync(req));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req, CancellationToken ct)
        => Ok(await auth.RefreshAsync(req.RefreshToken, ct));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest? req, CancellationToken ct)
    {
        await auth.LogoutAsync(req?.RefreshToken, User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        return Ok(new { message = "Logged out" });
    }
}
