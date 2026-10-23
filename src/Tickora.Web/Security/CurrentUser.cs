using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Tickora.Application;
using Tickora.Infrastructure.Persistence;

namespace Tickora.Web.Security;

public sealed class CurrentUser(AuthenticationStateProvider state, IHttpContextAccessor http, IDbContextFactory<ApplicationDbContext> factory) : ICurrentUser
{
    public async Task<Actor> GetAsync(CancellationToken ct = default)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) principal = (await state.GetAuthenticationStateAsync()).User;
        return await ResolveAsync(principal, ct);
    }

    public async Task<Actor> ResolveAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) throw new WorkflowException("unauthorized", "Please sign in to continue.", 401);
        await using var db = await factory.CreateDbContextAsync(ct);
        var account = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Active, ct);
        if (account is null || principal.FindFirstValue("AspNet.Identity.SecurityStamp") != account.SecurityStamp)
            throw new WorkflowException("unauthorized", "Your session has expired. Sign in again.", 401);
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == id select r.Name).ToListAsync(ct);
        return new(id, account.DisplayName, roles.Contains("Admin"), roles.Contains("Agent"),
            (await db.Memberships.Where(x => x.AgentId == id).Select(x => x.QueueId).ToListAsync(ct)).ToHashSet());
    }
}
