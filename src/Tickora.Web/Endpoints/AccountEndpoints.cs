using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Tickora.Infrastructure.Persistence;

namespace Tickora.Web.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccounts(this WebApplication app)
    {
        var accounts = app.MapGroup("/account/actions").RequireRateLimiting("account").AddEndpointFilter(async (context, next) =>
        {
            await context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context.HttpContext);
            return await next(context);
        });
        accounts.MapPost("/login", async (HttpContext h, SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users) =>
        {
            var f = await h.Request.ReadFormAsync(h.RequestAborted);
            var email = f["Email"].ToString();
            var user = await users.FindByEmailAsync(email);
            if (user is null || !user.Active) return Results.LocalRedirect("/account/login?error=Invalid%20email%20or%20password.");
            var result = await signIn.PasswordSignInAsync(user, f["Password"].ToString(), false, lockoutOnFailure: true);
            return result.Succeeded ? Results.LocalRedirect("/") : Results.LocalRedirect("/account/login?error=Sign%20in%20failed.%20Check%20your%20credentials%20or%20try%20again%20later.");
        });
        accounts.MapPost("/register", async (HttpContext h, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) =>
        {
            var f = await h.Request.ReadFormAsync(h.RequestAborted);
            var name = f["Name"].ToString().Trim(); var email = f["Email"].ToString().Trim();
            if (name.Length is < 2 or > 120 || email.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(email, out _)) return Results.LocalRedirect("/account/register?error=Enter%20a%20valid%20name%20and%20email.");
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = name };
            var result = await users.CreateAsync(user, f["Password"].ToString());
            if (!result.Succeeded) return Results.LocalRedirect("/account/register?error=" + Uri.EscapeDataString(string.Join(" ", result.Errors.Select(x => x.Description))));
            var role = await users.AddToRoleAsync(user, "Customer");
            if (!role.Succeeded) { await users.DeleteAsync(user); return Results.Problem("Account creation failed."); }
            await signIn.SignInAsync(user, false);
            return Results.LocalRedirect("/");
        });
        accounts.MapPost("/logout", async (SignInManager<ApplicationUser> signIn) => { await signIn.SignOutAsync(); return Results.LocalRedirect("/account/login"); }).RequireAuthorization();
        accounts.MapPost("/change-password", async (HttpContext h, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) =>
        {
            var account = await users.GetUserAsync(h.User);
            if (account is null || !account.Active) return Results.Unauthorized();
            var f = await h.Request.ReadFormAsync(h.RequestAborted);
            var result = await users.ChangePasswordAsync(account, f["CurrentPassword"].ToString(), f["NewPassword"].ToString());
            if (!result.Succeeded) return Results.LocalRedirect("/settings?error=" + Uri.EscapeDataString(string.Join(" ", result.Errors.Select(x => x.Description))));
            await signIn.RefreshSignInAsync(account); return Results.LocalRedirect("/settings?saved=true");
        }).RequireAuthorization();
        accounts.MapPost("/forgot", async (HttpContext h, UserManager<ApplicationUser> users, ResetEmailSender sender, IConfiguration config) =>
        {
            var f = await h.Request.ReadFormAsync(h.RequestAborted);
            var account = await users.FindByEmailAsync(f["Email"].ToString());
            if (account is { Active: true })
            {
                var token = await users.GeneratePasswordResetTokenAsync(account);
                // Configured origin prevents Host-header poisoning of password reset links.
                var origin = config["PublicOrigin"] ?? "http://localhost:5180";
                var link = $"{origin.TrimEnd('/')}/account/reset?email={Uri.EscapeDataString(account.Email!)}&token={Uri.EscapeDataString(token)}";
                await sender.SendAsync(account.Email!, link, h.RequestAborted);
            }
            return Results.LocalRedirect("/account/forgot?sent=true");
        });
        accounts.MapPost("/reset", async (HttpContext h, UserManager<ApplicationUser> users) =>
        {
            var f = await h.Request.ReadFormAsync(h.RequestAborted);
            var account = await users.FindByEmailAsync(f["Email"].ToString());
            if (account is null || !account.Active) return Results.LocalRedirect("/account/login?error=Reset%20link%20is%20invalid.");
            var result = await users.ResetPasswordAsync(account, f["Token"].ToString(), f["Password"].ToString());
            return result.Succeeded ? Results.LocalRedirect("/account/login?message=Password%20updated.%20Please%20sign%20in.") : Results.LocalRedirect("/account/login?error=Reset%20failed.%20Request%20a%20new%20link%20and%20use%20a%20strong%20password.");
        });
    }
}

public sealed class ResetEmailSender(IConfiguration config, IWebHostEnvironment environment)
{
    public async Task SendAsync(string email, string link, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config["Smtp:Host"]))
        {
            if (!environment.IsDevelopment()) throw new InvalidOperationException("SMTP must be configured to send password resets.");
            var folder = config["Smtp:DevelopmentOutbox"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "mail"); Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, $"{Guid.NewGuid():N}.txt"), $"To: {email}\nReset your TICKORA password:\n{link}", ct);
            return;
        }
        using var client = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 587))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"])
        };
        using var message = new MailMessage(config["Smtp:From"]!, email, "Reset your TICKORA password", $"Use this link to reset your password:\n{link}");
        await client.SendMailAsync(message, ct);
    }
}
