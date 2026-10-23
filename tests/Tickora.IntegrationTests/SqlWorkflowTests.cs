using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Tickora.Application;
using Tickora.Domain;
using Tickora.Infrastructure.BackgroundJobs;
using Tickora.Infrastructure.Persistence;

namespace Tickora.IntegrationTests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TICKORA_SQL_TESTS") != "1" && Environment.GetEnvironmentVariable("TICKORA_TESTCONTAINERS") != "1")
            Skip = "Requires SQL Server. Set TICKORA_SQL_TESTS=1 and TICKORA_TEST_CONNECTION, or TICKORA_TESTCONTAINERS=1 with Docker.";
    }
}

public sealed class SqlFixture : IAsyncLifetime
{
    private MsSqlContainer? container;
    private string? connection;
    public WebApplicationFactory<Program> App { get; private set; } = default!;
    public const string Password = "Test-Only!Ticket2026";
    private readonly string temporaryRoot = Path.Combine(Path.GetTempPath(), "TickoraTests", Guid.NewGuid().ToString("N"));
    public string MailFolder => Path.Combine(temporaryRoot, "mail");
    private readonly Dictionary<HttpClient, string> sessionCookies = [];
    public string Cookies(HttpClient client) => sessionCookies[client];
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("TICKORA_SQL_TESTS") != "1" && Environment.GetEnvironmentVariable("TICKORA_TESTCONTAINERS") != "1") return;
        if (Environment.GetEnvironmentVariable("TICKORA_TESTCONTAINERS") == "1")
        {
            container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await container.StartAsync(); connection = container.GetConnectionString();
        }
        else connection = Environment.GetEnvironmentVariable("TICKORA_TEST_CONNECTION") ?? throw new InvalidOperationException("TICKORA_TEST_CONNECTION is required.");
        var cs = new SqlConnectionStringBuilder(connection) { InitialCatalog = "TickoraTest_" + Guid.NewGuid().ToString("N") };
        connection = cs.ConnectionString;
        App = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development")
            .UseSetting("ConnectionStrings:TickoraDb", connection).UseSetting("Sla:MonitorEnabled", "false")
            .UseSetting("RateLimits:AccountPermitLimit", "1000").UseSetting("Storage:Root", Path.Combine(temporaryRoot, "attachments"))
            .UseSetting("Smtp:DevelopmentOutbox", MailFolder));
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(Password);
    }
    public async Task<HttpClient> Login(string email, string? password = null)
    {
        var client = App.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/account/login");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The login form must include an antiforgery token.");
        var login = await client.PostAsync("/account/actions/login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = email, ["Password"] = password ?? Password, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value) }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location?.OriginalString);
        sessionCookies[client] = string.Join("; ", login.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]));
        var token = await client.GetFromJsonAsync<JsonElement>("/api/v1/antiforgery");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        return client;
    }
    public async Task DisposeAsync()
    {
        if (connection is not null)
        {
            using var scope = App.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureDeletedAsync();
            await App.DisposeAsync();
        }
        if (container is not null) await container.DisposeAsync();
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TickoraTests")) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(temporaryRoot);
        if (!target.StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe temporary directory.");
        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
    }
}

public sealed partial class SqlWorkflowTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    [SqlFact]
    public async Task Anonymous_api_requests_return_401()
    {
        using var client = fixture.App.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/tickets")).StatusCode);
    }
    [SqlFact]
    public async Task Customer_cannot_read_another_customer_ticket_or_admin_screen()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = await db.Users.SingleAsync(x => x.Email == "customer@tickora.example");
        var other = await db.Tickets.FirstAsync(x => x.CustomerId != owner.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"/api/v1/tickets/{other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/admin/team")).StatusCode);
    }
    [SqlFact]
    public async Task Creation_reply_internal_note_and_assignment_persist_with_privacy()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        using var agent = await fixture.Login("agent@tickora.example");
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var queue = await db.Queues.FirstAsync(); var category = await db.Categories.FirstAsync();
        var created = await customer.PostAsJsonAsync("/api/v1/tickets", new CreateTicket("Integration workflow issue", "A reproducible problem in the SQL-backed workflow.", queue.Id, category.Id, TicketPriority.High, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        async Task<string> Version() => (await agent.GetFromJsonAsync<JsonElement>($"/api/v1/tickets/{id}")).GetProperty("ticket").GetProperty("rowVersion").GetString()!;
        var agentId = await db.Users.Where(x => x.Email == "agent@tickora.example").Select(x => x.Id).SingleAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/assign", new AssignTicket(agentId, await Version()))).StatusCode);
        var secret = "Private diagnosis " + Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/reply", new ReplyTicket(secret, true, Guid.NewGuid(), await Version()))).StatusCode);
        var afterNote = await db.Tickets.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Null(afterNote.FirstResponseAt);
        Assert.Equal(HttpStatusCode.NoContent, (await agent.PostAsJsonAsync($"/api/v1/tickets/{id}/reply", new ReplyTicket("A public answer for the customer", false, Guid.NewGuid(), await Version()))).StatusCode);
        var visible = await customer.GetStringAsync($"/api/v1/tickets/{id}");
        Assert.DoesNotContain(secret, visible); Assert.Contains("A public answer for the customer", visible);
        var saved = await db.Tickets.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.NotNull(saved.FirstResponseAt); Assert.Equal(agentId, saved.AssignedAgentId);
        Assert.True(await db.Notifications.AnyAsync(x => x.TicketId == id));
    }
    [SqlFact]
    public async Task Sql_rowversion_rejects_concurrent_updates()
    {
        using var scope = fixture.App.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var a = await factory.CreateDbContextAsync(); await using var b = await factory.CreateDbContextAsync();
        var first = await a.Tickets.FirstAsync(); var second = await b.Tickets.SingleAsync(x => x.Id == first.Id);
        first.Subject = "Changed by first writer"; await a.SaveChangesAsync();
        second.Subject = "Changed by stale second writer";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => b.SaveChangesAsync());
    }
    [SqlFact]
    public async Task Write_without_antiforgery_is_rejected()
    {
        using var customer = await fixture.Login("customer@tickora.example");
        customer.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var response = await customer.PostAsJsonAsync("/api/v1/tickets", new CreateTicket("Subject here", "Description here with enough detail", Guid.NewGuid(), Guid.NewGuid(), TicketPriority.Low, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [SqlFact]
    public async Task Private_attachment_cannot_be_downloaded_by_customer()
    {
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customerId = await db.Users.Where(x => x.Email == "customer@tickora.example").Select(x => x.Id).SingleAsync();
        var ticket = await db.Tickets.FirstAsync(x => x.CustomerId == customerId);
        var attachment = new TicketAttachment { TicketId = ticket.Id, FileName = "private.txt", Internal = true, StorageKey = Guid.NewGuid().ToString("N"), UploadedBy = customerId, ContentType = "text/plain", Length = 1, CreatedAt = DateTimeOffset.UtcNow };
        db.Set<TicketAttachment>().Add(attachment); await db.SaveChangesAsync();
        using var customer = await fixture.Login("customer@tickora.example");
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"/api/v1/tickets/{ticket.Id}/attachments/{attachment.Id}")).StatusCode);
    }
    [SqlFact]
    public async Task Repeated_sla_scans_do_not_duplicate_alerts()
    {
        using var scope = fixture.App.Services.CreateScope();
        var monitor = ActivatorUtilities.CreateInstance<SlaMonitoringService>(scope.ServiceProvider);
        await monitor.ScanAsync(CancellationToken.None);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var before = await db.Notifications.CountAsync(x => x.EventKey.StartsWith("sla:"));
        await monitor.ScanAsync(CancellationToken.None);
        Assert.True(before > 0);
        Assert.Equal(before, await db.Notifications.CountAsync(x => x.EventKey.StartsWith("sla:")));
    }
    [SqlFact]
    public async Task Seed_is_repeatable()
    {
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tickets = await db.Tickets.CountAsync(); var accounts = await db.Users.CountAsync();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(SqlFixture.Password);
        Assert.Equal(tickets, await db.Tickets.CountAsync()); Assert.Equal(accounts, await db.Users.CountAsync());
    }
    [Fact] public void Csv_escapes_formula_injection() => Assert.Equal("\"'=SUM(A1:A2)\"", Tickora.Web.Endpoints.ApiEndpoints.Csv("=SUM(A1:A2)"));
    [Fact] public void Extension_alone_does_not_authorize_upload() => Assert.Throws<WorkflowException>(() => Tickora.Web.Endpoints.AttachmentEndpoints.ValidateFile(".png", "not a PNG"u8.ToArray()));
}
