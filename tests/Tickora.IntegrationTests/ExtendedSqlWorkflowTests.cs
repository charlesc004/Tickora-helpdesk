using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickora.Application;
using Tickora.Domain;
using Tickora.Infrastructure.Persistence;

namespace Tickora.IntegrationTests;

public sealed partial class SqlWorkflowTests
{
    [SqlFact]
    public async Task Lookup_ids_match_guid_bound_form_values()
    {
        using var scope = fixture.App.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IHelpdeskQueries>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var admin = await db.Users.SingleAsync(x => x.Email == "admin@tickora.example");
        var lookups = await queries.LookupsAsync(new(admin.Id, admin.DisplayName, true, false, new HashSet<Guid>()), CancellationToken.None);
        Assert.NotEmpty(lookups.Queues);
        Assert.NotEmpty(lookups.Categories);
        Assert.All(lookups.Queues.Concat(lookups.Categories), choice => Assert.Equal(Guid.Parse(choice.Id).ToString(), choice.Id));
    }

    private async Task<Guid> CreateTicket(HttpClient customer, string subject)
    {
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var queue = await db.Queues.FirstAsync(); var category = await db.Categories.FirstAsync();
        var response = await customer.PostAsJsonAsync("/api/v1/tickets", new CreateTicket(subject,
            "A reproducible integration test request with enough detail.", queue.Id, category.Id, TicketPriority.High, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static async Task<string> Version(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<JsonElement>($"/api/v1/tickets/{id}")).GetProperty("ticket").GetProperty("rowVersion").GetString()!;
    private static async Task<HttpResponseMessage> SubmitForm(HttpClient client, string page, string action, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(page);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return await client.PostAsync(action, new FormUrlEncodedContent(fields));
    }

    [SqlFact]
    public async Task Dashboard_directory_and_filters_execute_against_real_sql()
    {
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var queries = scope.ServiceProvider.GetRequiredService<IHelpdeskQueries>();
        var admin = await db.Users.SingleAsync(x => x.Email == "admin@tickora.example");
        var actor = new Actor(admin.Id, admin.DisplayName, true, false, new HashSet<Guid>());
        var overview = await queries.DashboardAsync(actor, CancellationToken.None);
        Assert.Equal(await db.Tickets.CountAsync(), overview.Total);
        Assert.Equal(overview.Total, overview.Statuses.Sum(x => x.Count));
        Assert.Equal(overview.Total, overview.Categories.Sum(x => x.Count));
        var queue = await db.Queues.FirstAsync();
        var filter = new TicketFilter(Status: TicketStatus.Open, Queue: queue.Id, From: DateTimeOffset.UtcNow.AddDays(-40), To: DateTimeOffset.UtcNow.AddDays(1), PageSize: 3);
        var filtered = await queries.TicketsAsync(actor, filter, CancellationToken.None);
        var summary = await queries.DashboardAsync(actor, CancellationToken.None, filter);
        Assert.Equal(filtered.Total, summary.Total); Assert.InRange(filtered.Items.Count, 0, 3);
        Assert.All(filtered.Items, row => { Assert.Equal(TicketStatus.Open, row.Status); Assert.Equal(queue.Name, row.Queue); });
        var customers = await queries.CustomersAsync(actor, null, 1, CancellationToken.None);
        var customerCount = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where r.Name == "Customer" select ur.UserId).CountAsync();
        Assert.Equal(customerCount, customers.Total); Assert.Equal(12, customers.Items.Count);
        using var signedIn = await fixture.Login("admin@tickora.example");
        foreach (var path in new[] { "/", "/tickets", "/customers", "/reports", "/admin/team", "/admin/sla", "/knowledge/manage" })
        {
            var html = await signedIn.GetStringAsync(path);
            Assert.DoesNotContain("class=\"alert error\"", html);
        }
    }

    [SqlFact]
    public async Task Public_and_private_uploads_persist_and_download_with_resource_authorization()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        using var agent = await fixture.Login("agent@tickora.example");
        var id = await CreateTicket(customer, "File authorization workflow");
        async Task Upload(HttpClient actor, string name, bool privateFile)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent("Safe UTF-8 attachment content"u8.ToArray()), "file", name);
            content.Add(new StringContent(await Version(actor, id)), "version");
            content.Add(new StringContent(privateFile.ToString()), "internal");
            Assert.Equal(HttpStatusCode.Created, (await actor.PostAsync($"/api/v1/tickets/{id}/attachments", content)).StatusCode);
        }
        await Upload(customer, "customer.txt", false); await Upload(agent, "team.txt", true);
        var agentView = await agent.GetFromJsonAsync<JsonElement>($"/api/v1/tickets/{id}");
        var files = agentView.GetProperty("ticket").GetProperty("attachments").EnumerateArray().ToArray();
        var publicId = files.Single(x => !x.GetProperty("internal").GetBoolean()).GetProperty("id").GetGuid();
        var privateId = files.Single(x => x.GetProperty("internal").GetBoolean()).GetProperty("id").GetGuid();
        var customerView = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/tickets/{id}");
        Assert.Single(customerView.GetProperty("ticket").GetProperty("attachments").EnumerateArray());
        var download = await customer.GetAsync($"/api/v1/tickets/{id}/attachments/{publicId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("Safe UTF-8 attachment content", await download.Content.ReadAsStringAsync());
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"/api/v1/tickets/{id}/attachments/{privateId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await agent.GetAsync($"/api/v1/tickets/{id}/attachments/{privateId}")).StatusCode);
        using var invalid = new MultipartFormDataContent();
        invalid.Add(new ByteArrayContent("Not a PNG"u8.ToArray()), "file", "forged.png");
        invalid.Add(new StringContent(await Version(customer, id)), "version");
        Assert.Equal(HttpStatusCode.BadRequest, (await customer.PostAsync($"/api/v1/tickets/{id}/attachments", invalid)).StatusCode);
    }

    [SqlFact]
    public async Task Registration_password_change_and_reset_use_real_identity_and_private_outbox()
    {
        using var client = fixture.App.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"journey-{Guid.NewGuid():N}@example.test";
        var registered = await SubmitForm(client, "/account/register", "/account/actions/register", new()
        { ["Name"] = "Identity Journey", ["Email"] = email, ["Password"] = SqlFixture.Password, ["Role"] = "Admin" });
        Assert.Equal("/", registered.Headers.Location?.OriginalString);
        using var scope = fixture.App.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var account = (await users.FindByEmailAsync(email))!;
        Assert.Equal(new[] { "Customer" }, await users.GetRolesAsync(account));
        const string changed = "Changed-Only!Ticket2026", reset = "Reset-Only!Ticket2026";
        var change = await SubmitForm(client, "/settings", "/account/actions/change-password", new()
        { ["CurrentPassword"] = SqlFixture.Password, ["NewPassword"] = changed });
        Assert.Equal("/settings?saved=true", change.Headers.Location?.OriginalString);
        await SubmitForm(client, "/settings", "/account/actions/logout", new());
        var forgot = await SubmitForm(client, "/account/forgot", "/account/actions/forgot", new() { ["Email"] = email });
        Assert.Equal("/account/forgot?sent=true", forgot.Headers.Location?.OriginalString);
        var mail = Directory.GetFiles(fixture.MailFolder).Select(File.ReadAllText).Single(x => x.StartsWith($"To: {email}\n", StringComparison.Ordinal));
        var link = new Uri(mail.Split('\n').Last()); var query = QueryHelpers.ParseQuery(link.Query);
        var result = await SubmitForm(client, "/account/reset", "/account/actions/reset", new()
        { ["Email"] = email, ["Token"] = query["token"].ToString(), ["Password"] = reset });
        Assert.StartsWith("/account/login?message=", result.Headers.Location?.OriginalString);
        using var signedIn = await fixture.Login(email, reset);
        Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync("/api/v1/tickets")).StatusCode);
    }

    [SqlFact]
    public async Task Stale_api_updates_return_409_and_notifications_are_owned_and_paginated()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        using var agent = await fixture.Login("agent@tickora.example");
        var id = await CreateTicket(customer, "Concurrency through the HTTP boundary");
        var version = await Version(agent, id);
        var first = await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/transition", new TransitionTicket(TicketStatus.InProgress, version));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        var stale = await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/transition", new TransitionTicket(TicketStatus.Resolved, version));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var notifications = await customer.GetFromJsonAsync<JsonElement>("/api/v1/notifications?pageSize=1&page=1");
        Assert.Equal(1, notifications.GetProperty("size").GetInt32());
        Assert.Single(notifications.GetProperty("items").EnumerateArray());
        var notificationId = notifications.GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsync($"/api/v1/notifications/{notificationId}/read", null)).StatusCode);
        using var scope = fixture.App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.NotNull((await db.Notifications.SingleAsync(x => x.Id == notificationId)).ReadAt);
        var other = await db.Notifications.FirstAsync(x => x.RecipientId != notifications.GetProperty("items")[0].GetProperty("recipientId").GetString() && x.ReadAt == null);
        await customer.PostAsync($"/api/v1/notifications/{other.Id}/read", null);
        Assert.Null((await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == other.Id)).ReadAt);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/reports/export")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await agent.GetAsync("/api/v1/reports/export")).StatusCode);
    }
}
