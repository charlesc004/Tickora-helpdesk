using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickora.Application;
using Tickora.Domain;
using Tickora.Infrastructure.Persistence;

namespace Tickora.Infrastructure;

public sealed class AdministrationService(IDbContextFactory<ApplicationDbContext> factory, IServiceScopeFactory scopes, ICurrentUser user, IClock clock) : IAdministration
{
    private async Task<Actor> RequireAsync(bool staff, CancellationToken ct)
    {
        var actor = await user.GetAsync(ct);
        if (!actor.Admin && !(staff && actor.Agent)) throw TicketService.Forbidden();
        return actor;
    }
    public async Task<IReadOnlyList<TeamMember>> TeamAsync(CancellationToken ct = default)
    {
        await RequireAsync(false, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var members = await (from u in db.Users
                             join ur in db.UserRoles on u.Id equals ur.UserId
                             join r in db.Roles on ur.RoleId equals r.Id
                             where r.Name == "Agent" || r.Name == "Admin"
                             orderby u.DisplayName
                             select new { u.Id, u.DisplayName, u.Email, u.Active, Role = r.Name }).ToListAsync(ct);
        var memberships = await db.Memberships.AsNoTracking().ToListAsync(ct);
        return members.Select(x => new TeamMember(x.Id, x.DisplayName, x.Email!, x.Active, x.Role!, memberships.Where(m => m.AgentId == x.Id).Select(m => m.QueueId).ToList())).ToList();
    }
    public async Task CreateAgentAsync(string name, string email, string password, CancellationToken ct = default)
    {
        await RequireAsync(false, ct); TicketService.Text(name, 2, 120, "Name");
        await using var scope = scopes.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var account = new ApplicationUser { DisplayName = name.Trim(), UserName = email.Trim(), Email = email.Trim(), EmailConfirmed = true };
        var result = await manager.CreateAsync(account, password);
        if (!result.Succeeded) throw TicketService.Invalid(string.Join(" ", result.Errors.Select(e => e.Description)));
        var role = await manager.AddToRoleAsync(account, "Agent");
        if (!role.Succeeded) { await manager.DeleteAsync(account); throw TicketService.Invalid("Unable to create agent role."); }
    }
    public async Task SetMembershipAsync(string userId, Guid queueId, bool member, CancellationToken ct = default)
    {
        await RequireAsync(false, ct); await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Queues.AnyAsync(x => x.Id == queueId, ct) || !await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == userId && (r.Name == "Agent" || r.Name == "Admin") select ur).AnyAsync(ct)) throw TicketService.Invalid("Select a queue and a staff member.");
        var existing = await db.Memberships.FindAsync([queueId, userId], ct);
        if (member && existing is null) db.Memberships.Add(new QueueMembership { AgentId = userId, QueueId = queueId });
        if (!member && existing is not null)
        {
            if (await db.Tickets.AnyAsync(t => t.QueueId == queueId && t.AssignedAgentId == userId && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved, ct)) throw TicketService.Invalid("Reassign active tickets before removing queue access.");
            db.Memberships.Remove(existing);
        }
        await db.SaveChangesAsync(ct);
    }
    public async Task SetActiveAsync(string userId, bool active, CancellationToken ct = default)
    {
        var actor = await RequireAsync(false, ct);
        if (actor.Id == userId) throw TicketService.Invalid("You cannot deactivate your own account.");
        await using var scope = scopes.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var account = await manager.FindByIdAsync(userId) ?? throw TicketService.Invalid("Account not found.");
        if (await manager.IsInRoleAsync(account, "Admin")) throw TicketService.Invalid("Administrator deactivation requires a separate recovery procedure.");
        account.Active = active;
        var result = await manager.UpdateAsync(account);
        if (!result.Succeeded) throw TicketService.Invalid("Account update failed.");
        await manager.UpdateSecurityStampAsync(account);
    }
    public async Task SetRoleAsync(string userId, string role, CancellationToken ct = default)
    {
        var actor = await RequireAsync(false, ct);
        if (role is not ("Agent" or "Customer" or "Admin")) throw TicketService.Invalid("Unknown account role.");
        if (actor.Id == userId) throw TicketService.Invalid("You cannot change your own administrator role.");
        await using var scope = scopes.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = await manager.FindByIdAsync(userId) ?? throw TicketService.Invalid("Account not found.");
        if (role == "Customer" && await db.Tickets.AnyAsync(t => t.AssignedAgentId == userId && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed, ct)) throw TicketService.Invalid("Reassign active tickets before changing this role.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sp_getapplock @Resource = 'Tickora:RoleChanges', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000; IF @r < 0 THROW 51001, 'Role update busy', 1;", ct);
        // Recheck after acquiring the cross-process lock: another administrator may have revoked this session.
        await RequireAsync(false, ct);
        var existing = await manager.GetRolesAsync(account);
        if (existing.Count > 0 && !(await manager.RemoveFromRolesAsync(account, existing)).Succeeded) throw TicketService.Invalid("Role change failed.");
        if (!(await manager.AddToRoleAsync(account, role)).Succeeded) throw TicketService.Invalid("Role change failed.");
        if (role == "Customer") { db.Memberships.RemoveRange(db.Memberships.Where(m => m.AgentId == userId)); await db.SaveChangesAsync(ct); }
        await manager.UpdateSecurityStampAsync(account);
        await transaction.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<SupportQueue>> QueuesAsync(CancellationToken ct = default)
    {
        await RequireAsync(false, ct); await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Queues.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    }
    public async Task SaveQueueAsync(Guid? id, string name, bool active, string? version, CancellationToken ct = default)
    {
        await RequireAsync(false, ct); TicketService.Text(name, 2, 100, "Queue name"); await using var db = await factory.CreateDbContextAsync(ct);
        var queue = id.HasValue ? await db.Queues.SingleAsync(x => x.Id == id, ct) : new SupportQueue();
        if (id.HasValue) CheckVersion(queue.RowVersion, version);
        if (!id.HasValue) db.Queues.Add(queue);
        queue.Name = name.Trim(); queue.Active = active; await SaveAsync(db, ct);
    }
    public async Task<IReadOnlyList<SlaPolicy>> PoliciesAsync(CancellationToken ct = default)
    {
        await RequireAsync(false, ct); await using var db = await factory.CreateDbContextAsync(ct);
        return await db.SlaPolicies.AsNoTracking().OrderBy(x => x.Priority).ToListAsync(ct);
    }
    public async Task SavePolicyAsync(SlaPolicy input, CancellationToken ct = default)
    {
        await RequireAsync(false, ct);
        if (input.FirstResponseMinutes is < 1 or > 100000 || input.ResolutionMinutes < input.FirstResponseMinutes || input.ResolutionMinutes > 100000) throw TicketService.Invalid("Use positive SLA targets; resolution must be at least the response target.");
        try { (JsonSerializer.Deserialize<BusinessCalendar>(input.CalendarJson) ?? throw new ArgumentException()).Validate(); }
        catch (Exception e) when (e is JsonException or ArgumentException or TimeZoneNotFoundException or InvalidTimeZoneException) { throw TicketService.Invalid("Invalid business calendar JSON or time zone."); }
        await using var db = await factory.CreateDbContextAsync(ct);
        var policy = await db.SlaPolicies.SingleAsync(x => x.Priority == input.Priority, ct);
        CheckVersion(policy.RowVersion, Convert.ToBase64String(input.RowVersion));
        policy.FirstResponseMinutes = input.FirstResponseMinutes; policy.ResolutionMinutes = input.ResolutionMinutes;
        policy.PauseWhileWaiting = input.PauseWhileWaiting; policy.CalendarJson = input.CalendarJson; await SaveAsync(db, ct);
    }
    public async Task SaveArticleAsync(KnowledgeArticle input, CancellationToken ct = default)
    {
        var actor = await RequireAsync(true, ct); TicketService.Text(input.Title, 5, 180, "Title"); TicketService.Text(input.Body, 20, 30000, "Article");
        TicketService.Text(input.Summary, 5, 400, "Summary"); TicketService.Text(input.Category, 2, 100, "Category");
        if (!Regex.IsMatch(input.Slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant) || input.Slug.Length > 180) throw TicketService.Invalid("Slug must use lowercase letters, digits, and hyphens.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var article = await db.Articles.SingleOrDefaultAsync(x => x.Id == input.Id, ct);
        if (article is null) { article = new KnowledgeArticle { AuthorId = actor.Id }; db.Articles.Add(article); }
        else CheckVersion(article.RowVersion, Convert.ToBase64String(input.RowVersion));
        article.Title = input.Title.Trim(); article.Slug = input.Slug; article.Summary = input.Summary.Trim(); article.Body = input.Body.Trim();
        article.Category = input.Category.Trim(); article.Published = input.Published; article.Featured = input.Featured; article.UpdatedAt = clock.UtcNow;
        await SaveAsync(db, ct);
    }
    public async Task CreateResponseAsync(string title, string body, bool active = true, CancellationToken ct = default)
    {
        await RequireAsync(true, ct); TicketService.Text(title, 2, 120, "Title"); TicketService.Text(body, 1, 10000, "Response");
        await using var db = await factory.CreateDbContextAsync(ct);
        var response = new CannedResponse { Active = active };
        db.Responses.Add(response);
        response.Title = title.Trim(); response.Body = body.Trim(); await SaveAsync(db, ct);
    }
    public async Task SaveCategoryAsync(Guid? id, string name, bool active, CancellationToken ct = default, string? version = null)
    {
        await RequireAsync(false, ct); TicketService.Text(name, 2, 100, "Category name");
        await using var db = await factory.CreateDbContextAsync(ct);
        var category = id.HasValue ? await db.Categories.SingleAsync(x => x.Id == id, ct) : new TicketCategory();
        if (id.HasValue) CheckVersion(category.RowVersion, version);
        if (!id.HasValue) db.Categories.Add(category);
        category.Name = name.Trim(); category.Active = active; await SaveAsync(db, ct);
    }
    public async Task<IReadOnlyList<CannedResponse>> ResponsesAsync(CancellationToken ct = default)
    {
        await RequireAsync(true, ct); await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Responses.AsNoTracking().OrderBy(x => x.Title).ToListAsync(ct);
    }
    public async Task UpdateResponseAsync(CannedResponse input, CancellationToken ct = default)
    {
        await RequireAsync(true, ct); TicketService.Text(input.Title, 2, 120, "Title"); TicketService.Text(input.Body, 1, 10000, "Response");
        await using var db = await factory.CreateDbContextAsync(ct);
        var response = await db.Responses.SingleOrDefaultAsync(x => x.Id == input.Id, ct) ?? throw TicketService.Invalid("Saved response not found.");
        CheckVersion(response.RowVersion, Convert.ToBase64String(input.RowVersion));
        response.Title = input.Title.Trim(); response.Body = input.Body.Trim(); response.Active = input.Active;
        await SaveAsync(db, ct);
    }
    public async Task<IReadOnlyList<TicketCategory>> CategoriesAsync(CancellationToken ct = default)
    {
        await RequireAsync(false, ct); await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    }
    private static void CheckVersion(byte[] current, string? expected)
    {
        if (Convert.ToBase64String(current) != expected) throw new WorkflowException("conflict", "Record changed. Refresh before saving.", 409);
    }
    private static async Task SaveAsync(ApplicationDbContext db, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new WorkflowException("conflict", "Record changed. Refresh before saving.", 409); }
        catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { throw new WorkflowException("duplicate", "This name or slug is already in use.", 409); }
    }
}
