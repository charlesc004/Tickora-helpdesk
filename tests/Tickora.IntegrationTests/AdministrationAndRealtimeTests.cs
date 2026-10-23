using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickora.Application;
using Tickora.Domain;
using Tickora.Infrastructure.BackgroundJobs;
using Tickora.Infrastructure.Persistence;

namespace Tickora.IntegrationTests;

public sealed partial class SqlWorkflowTests
{
    [SqlFact]
    public async Task Queue_membership_and_deactivation_are_enforced_on_live_sessions()
    {
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"restricted-{Guid.NewGuid():N}@example.test";
        var restricted = new ApplicationUser { DisplayName = "Restricted Agent", Email = email, UserName = email };
        Assert.True((await users.CreateAsync(restricted, SqlFixture.Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(restricted, "Agent")).Succeeded);
        var queues = await db.Queues.OrderBy(x => x.Name).ToListAsync();
        db.Memberships.Add(new QueueMembership { AgentId = restricted.Id, QueueId = queues[0].Id });
        await db.SaveChangesAsync();
        using var agent = await fixture.Login(email);
        using var customer = await fixture.Login("customer@tickora.example");
        var category = await db.Categories.FirstAsync();
        var created = await customer.PostAsJsonAsync("/api/v1/tickets", new CreateTicket("Queue access boundary", "This ticket is intentionally in a different queue.", queues[1].Id, category.Id, TicketPriority.High, Guid.NewGuid()));
        var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await agent.GetAsync($"/api/v1/tickets/{id}")).StatusCode);
        using var admin = await fixture.Login("admin@tickora.example");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/v1/tickets/{id}/assign", new AssignTicket(restricted.Id, await Version(admin, id)))).StatusCode);
        restricted.Active = false; Assert.True((await users.UpdateAsync(restricted)).Succeeded);
        Assert.True((await users.UpdateSecurityStampAsync(restricted)).Succeeded);
        Assert.Equal(HttpStatusCode.Unauthorized, (await agent.GetAsync("/api/v1/tickets")).StatusCode);
    }

    [SqlFact]
    public async Task Articles_templates_categories_and_administrator_roles_persist_with_concurrency_checks()
    {
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var adminAccount = (await users.FindByEmailAsync("admin@tickora.example"))!;
        var principal = await signIn.CreateUserPrincipalAsync(adminAccount);
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = principal };
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<IAdministration>();
            var queries = scope.ServiceProvider.GetRequiredService<IHelpdeskQueries>();
            var current = await scope.ServiceProvider.GetRequiredService<ICurrentUser>().GetAsync();
            var slug = "private-guide-" + Guid.NewGuid().ToString("N");
            await service.SaveArticleAsync(new KnowledgeArticle { Title = "A private operations guide", Slug = slug, Summary = "A guide for testing the publish workflow.", Body = "Plain text content with <script>alert('encoded')</script> in an example.", Category = "Getting started" });
            Assert.Null(await queries.ArticleAsync(slug, CancellationToken.None));
            var article = await db.Articles.AsNoTracking().SingleAsync(x => x.Slug == slug);
            article.Published = true; await service.SaveArticleAsync(article);
            Assert.NotNull(await queries.ArticleAsync(slug, CancellationToken.None));
            var staleArticle = await Assert.ThrowsAsync<WorkflowException>(() => service.SaveArticleAsync(article));
            Assert.Equal(409, staleArticle.Status);
            var title = "A saved response " + Guid.NewGuid().ToString("N");
            await service.CreateResponseAsync(title, "A reusable reply for the customer.");
            var response = (await service.ResponsesAsync()).Single(x => x.Title == title);
            var oldVersion = response.RowVersion.ToArray(); response.Body = "Updated reusable reply.";
            await service.UpdateResponseAsync(response);
            response.RowVersion = oldVersion;
            Assert.Equal(409, (await Assert.ThrowsAsync<WorkflowException>(() => service.UpdateResponseAsync(response))).Status);
            response = (await service.ResponsesAsync()).Single(x => x.Title == title); response.Active = false;
            await service.UpdateResponseAsync(response);
            Assert.DoesNotContain((await queries.LookupsAsync(current, CancellationToken.None)).Responses, x => x.Id == response.Id);
            var category = (await service.CategoriesAsync()).First();
            await service.SaveCategoryAsync(category.Id, category.Name + " updated", category.Active, version: Convert.ToBase64String(category.RowVersion));
            Assert.Equal(409, (await Assert.ThrowsAsync<WorkflowException>(() => service.SaveCategoryAsync(category.Id, category.Name, category.Active, version: Convert.ToBase64String(category.RowVersion)))).Status);
            await service.CreateAgentAsync("Role Test Agent", $"role-{Guid.NewGuid():N}@example.test", SqlFixture.Password);
            var account = await db.Users.SingleAsync(x => x.DisplayName == "Role Test Agent");
            await service.SetRoleAsync(account.Id, "Customer");
            await db.Entry(account).ReloadAsync();
            Assert.True(await users.IsInRoleAsync(account, "Customer"));
            await Assert.ThrowsAsync<WorkflowException>(() => service.SetRoleAsync(adminAccount.Id, "Customer"));
        }
        finally { accessor.HttpContext = previous; }
    }

    [SqlFact]
    public async Task Unassigned_sla_alerts_reach_queue_staff_once_with_one_audit_event()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        var id = await CreateTicket(customer, "Unassigned SLA escalation");
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ticket = await db.Tickets.SingleAsync(x => x.Id == id);
        ticket.FirstResponseDue = DateTimeOffset.UtcNow.AddHours(-1); ticket.ResolutionDue = DateTimeOffset.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        var monitor = ActivatorUtilities.CreateInstance<SlaMonitoringService>(scope.ServiceProvider);
        await monitor.ScanAsync(CancellationToken.None); await monitor.ScanAsync(CancellationToken.None);
        var events = await db.Set<TicketActivity>().Where(x => x.TicketId == id && x.EventKey != null).ToListAsync();
        Assert.Equal(2, events.Count);
        foreach (var activity in events)
        {
            var recipients = await db.Notifications.Where(x => x.EventKey == activity.EventKey).Select(x => x.RecipientId).ToListAsync();
            Assert.True(recipients.Count >= 5); Assert.Equal(recipients.Count, recipients.Distinct().Count());
        }
    }

    [SqlFact]
    public async Task Realtime_hints_are_user_scoped_and_internal_notes_do_not_emit_customer_events()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        using var other = await fixture.Login("customer2@tickora.example");
        using var agent = await fixture.Login("agent@tickora.example");
        var id = await CreateTicket(customer, "Realtime privacy integration");
        HubConnection Connect(HttpClient session) => new HubConnectionBuilder().WithUrl("http://localhost/hubs/notifications", options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => fixture.App.Server.CreateHandler();
            options.Headers["Cookie"] = fixture.Cookies(session);
        }).Build();
        await using var ownConnection = Connect(customer); await using var otherConnection = Connect(other);
        var ownHint = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var otherHints = 0;
        ownConnection.On("notificationsChanged", () => ownHint.TrySetResult());
        otherConnection.On("notificationsChanged", () => Interlocked.Increment(ref otherHints));
        await ownConnection.StartAsync(); await otherConnection.StartAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/reply", new ReplyTicket("Private team note", true, Guid.NewGuid(), await Version(agent, id)))).StatusCode);
        await Assert.ThrowsAsync<TimeoutException>(() => ownHint.Task.WaitAsync(TimeSpan.FromMilliseconds(250)));
        Assert.Equal(HttpStatusCode.NoContent, (await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/reply", new ReplyTicket("Public customer answer", false, Guid.NewGuid(), await Version(agent, id)))).StatusCode);
        await ownHint.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, otherHints);
    }
}
