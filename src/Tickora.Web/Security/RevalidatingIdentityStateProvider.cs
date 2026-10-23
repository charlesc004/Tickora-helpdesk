using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Tickora.Infrastructure.Persistence;

namespace Tickora.Web.Security;

public sealed class RevalidatingIdentityStateProvider(ILoggerFactory logger, IServiceScopeFactory scopes)
    : RevalidatingServerAuthenticationStateProvider(logger)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var account = await users.GetUserAsync(state.User);
        return account is { Active: true } && state.User.FindFirstValue("AspNet.Identity.SecurityStamp") == await users.GetSecurityStampAsync(account);
    }
}
