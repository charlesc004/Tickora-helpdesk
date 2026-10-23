using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tickora.Application;
using Tickora.Infrastructure;
using Tickora.Infrastructure.BackgroundJobs;
using Tickora.Infrastructure.Persistence;
using Tickora.Infrastructure.Storage;
using Tickora.Web.Components;
using Tickora.Web.Endpoints;
using Tickora.Web.Hubs;
using Tickora.Web.Security;

var builder = WebApplication.CreateBuilder(args.Where(a => a is not ("--migrate" or "--seed")).ToArray());
var connection = builder.Configuration.GetConnectionString("TickoraDb") ?? throw new InvalidOperationException("ConnectionStrings:TickoraDb is required.");
if (!builder.Environment.IsDevelopment() && (!Uri.TryCreate(builder.Configuration["PublicOrigin"], UriKind.Absolute, out var origin) || origin.Scheme != "https"))
    throw new InvalidOperationException("Set PublicOrigin to the externally accessible HTTPS origin in production.");
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection().SetApplicationName("Tickora").PersistKeysToFileSystem(new DirectoryInfo(keysPath));
builder.Services.AddTickoraInfrastructure(connection);
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 12; o.User.RequireUniqueEmail = true;
    o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/account/login"; o.AccessDeniedPath = "/account/login";
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401; else ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(o => { o.AddPolicy("Staff", p => p.RequireRole("Agent", "Admin")); o.AddPolicy("Admin", p => p.RequireRole("Admin")); });
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityStateProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(s => s.GetRequiredService<CurrentUser>());
builder.Services.AddScoped<IAdministration, AdministrationService>();
builder.Services.AddScoped<ResetEmailSender>(); builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddSignalR(); builder.Services.AddSingleton<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddSingleton<NotificationPulse>();
builder.Services.AddSingleton<IFileStorage>(new LocalFileStorage(builder.Configuration["Storage:Root"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "attachments")));
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = AttachmentEndpoints.MaxBytes + 65536);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Clamp(builder.Configuration.GetValue("RateLimits:AccountPermitLimit", 10), 1, 1000), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<WorkflowExceptionHandler>(); builder.Services.AddOpenApi();
if (builder.Configuration.GetValue("Sla:MonitorEnabled", true)) builder.Services.AddHostedService<SlaMonitoringService>();
var app = builder.Build();
if (args.Contains("--migrate") || args.Contains("--seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (args.Contains("--migrate")) await db.Database.MigrateAsync();
    if (args.Contains("--seed"))
    {
        if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo seeding is restricted to Development.");
        var password = builder.Configuration["Demo:Password"] ?? throw new InvalidOperationException("Set Demo__Password to a strong development-only password.");
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(password);
    }
    return;
}
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (h, next) => { h.Response.Headers["X-Content-Type-Options"] = "nosniff"; h.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin"; h.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'; object-src 'none'; base-uri 'self'"; await next(); });
app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization(); app.UseAntiforgery();
app.MapStaticAssets(); app.MapAccounts(); app.MapTickoraApi(); app.MapAttachments();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (IDbContextFactory<ApplicationDbContext> f, CancellationToken ct) =>
{
    try { await using var db = await f.CreateDbContextAsync(ct); return await db.Database.CanConnectAsync(ct) && !(await db.Database.GetPendingMigrationsAsync(ct)).Any() ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
    catch { return Results.StatusCode(503); }
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
public partial class Program;
