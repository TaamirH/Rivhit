using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rivhit.Api.Attendance;
using Rivhit.Api.Auth;
using Rivhit.Api.Data;
using Rivhit.Api.Reports;
using Rivhit.Api.Time;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Rivhit API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Persistence
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing ConnectionStrings:Default. Set it in appsettings or env var ConnectionStrings__Default.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Identity (users + roles)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false; // keep take-home onboarding simple
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// JWT auth
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtTokenService>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Issuer) ||
    string.IsNullOrWhiteSpace(jwt.Audience) ||
    string.IsNullOrWhiteSpace(jwt.SigningKey) ||
    jwt.SigningKey.Length < 32)
{
    throw new InvalidOperationException("JWT config is missing/too short. Set Jwt:Issuer, Jwt:Audience, and a Jwt:SigningKey (>= 32 chars).");
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey ?? ""));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(10),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Authoritative time provider (server-side external time source)
builder.Services.AddHttpClient<ITimeProvider, TimeApiIoTimeProvider>(client =>
{
    client.BaseAddress = new Uri("https://timeapi.io/api/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Rivhit/1.0");
});

builder.Services.AddScoped<ClockService>();
builder.Services.AddScoped<AdminShiftService>();
builder.Services.AddScoped<AdminReportService>();

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services, app.Configuration, CancellationToken.None);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

app.UseAuthentication();
app.UseAuthorization();

// Auth
var authGroup = app.MapGroup("/auth").WithTags("Auth");

authGroup.MapPost("/register", async (
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService) =>
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required." });
        }

        var user = new ApplicationUser { UserName = email, Email = email };
        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            return Results.BadRequest(new { errors = create.Errors.Select(e => new { e.Code, e.Description }) });
        }

        await userManager.AddToRoleAsync(user, "Employee");
        var roles = (await userManager.GetRolesAsync(user)).ToArray();

        var (token, expiresAt) = tokenService.CreateAccessToken(user, roles);
        return Results.Ok(new AuthResponse(token, expiresAt, user.Id, user.Email ?? "", roles));
    })
    .AllowAnonymous()
    .WithName("Register");

authGroup.MapPost("/login", async (
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService) =>
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var ok = await userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            return Results.Unauthorized();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var (token, expiresAt) = tokenService.CreateAccessToken(user, roles);
        return Results.Ok(new AuthResponse(token, expiresAt, user.Id, user.Email ?? "", roles));
    })
    .AllowAnonymous()
    .WithName("Login");

authGroup.MapGet("/me", async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return Results.Ok(new { user.Id, user.Email, Roles = roles });
    })
    .RequireAuthorization()
    .WithName("Me");

// Attendance (employee self-service)
var attendance = app.MapGroup("/").WithTags("Attendance").RequireAuthorization();

attendance.MapPost("/punches/clock-in", async (HttpContext ctx, ClaimsPrincipal principal, ClockService clock, CancellationToken ct) =>
{
    var userId = principal.GetRequiredUserId();
    var idempotencyKey = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();

    try
    {
        var result = await clock.ClockInAsync(userId, idempotencyKey, ct);
        return Results.Ok(result);
    }
    catch (TimeProviderException ex)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Time service unavailable", detail: ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
})
.WithName("ClockIn");

attendance.MapPost("/punches/clock-out", async (HttpContext ctx, ClaimsPrincipal principal, ClockService clock, CancellationToken ct) =>
{
    var userId = principal.GetRequiredUserId();
    var idempotencyKey = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();

    try
    {
        var result = await clock.ClockOutAsync(userId, idempotencyKey, ct);
        return Results.Ok(result);
    }
    catch (TimeProviderException ex)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Time service unavailable", detail: ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
})
.WithName("ClockOut");

attendance.MapGet("/me/status", async (ClaimsPrincipal principal, ClockService clock, CancellationToken ct) =>
{
    var userId = principal.GetRequiredUserId();
    return Results.Ok(await clock.GetStatusAsync(userId, ct));
})
.WithName("MyStatus");

attendance.MapGet("/me/shifts", async (ClaimsPrincipal principal, ClockService clock, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken ct) =>
{
    var userId = principal.GetRequiredUserId();
    return Results.Ok(await clock.ListMyShiftsAsync(userId, fromUtc, toUtc, ct));
})
.WithName("MyShifts");

// Admin
var admin = app.MapGroup("/admin")
    .WithTags("Admin")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

admin.MapGet("/open-shifts", async (AdminShiftService adminShiftService, CancellationToken ct) =>
{
    return Results.Ok(await adminShiftService.ListOpenShiftsAsync(ct));
})
.WithName("AdminOpenShifts");

admin.MapPost("/shifts/{shiftId:guid}/close", async (
    Guid shiftId,
    CloseShiftRequest request,
    ClaimsPrincipal principal,
    AdminShiftService adminShiftService,
    CancellationToken ct) =>
{
    var adminUserId = principal.GetRequiredUserId();
    try
    {
        return Results.Ok(await adminShiftService.CloseShiftAsync(adminUserId, shiftId, request.Reason, ct));
    }
    catch (TimeProviderException ex)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Time service unavailable", detail: ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
})
.WithName("AdminCloseShift");

admin.MapGet("/reports/shifts.csv", async (AdminReportService reports, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken ct) =>
{
    var csv = await reports.ExportShiftsCsvAsync(fromUtc, toUtc, ct);
    return Results.Text(csv, "text/csv", Encoding.UTF8);
})
.WithName("AdminExportShiftsCsv");

// Debug: verify authoritative time (for dev only)
app.MapGet("/debug/time/zurich", async (Rivhit.Api.Time.ITimeProvider timeProvider, CancellationToken ct) =>
{
    var snapshot = await timeProvider.GetZurichNowAsync(ct);
    return Results.Ok(new
    {
        snapshot.UtcDateTime,
        snapshot.ZurichDateTime,
        snapshot.UnixTimeSeconds,
        snapshot.Timezone,
        snapshot.UtcOffset
    });
})
.WithName("DebugGetZurichTime")
.WithOpenApi();

app.Run();

public sealed record CloseShiftRequest(string Reason);
