using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SevaSetu.Api.Middleware;
using SevaSetu.Application.Common;
using SevaSetu.Infrastructure;
using SevaSetu.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors(o => o.AddPolicy("spa", p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5180"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
for (var attempt = 1; ; attempt++)
{
    try
    {
        await DbSeeder.SeedAsync(app.Services);
        break;
    }
    catch (Exception ex) when (attempt < 5)
    {
        app.Logger.LogWarning(ex, "Database is not ready (attempt {Attempt}).", attempt);
        await Task.Delay(TimeSpan.FromSeconds(15));
    }
}
app.Run();
