using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Tickora.E2ETests;

public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TICKORA_E2E") != "1") Skip = "Requires a running, migrated, seeded application and Playwright Chromium. Set TICKORA_E2E=1.";
    }
}
public sealed class BrowserTheoryAttribute : TheoryAttribute
{
    public BrowserTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("TICKORA_E2E") != "1") Skip = "Requires a running, migrated, seeded application. Set TICKORA_E2E=1.";
    }
}
public sealed partial class SupportJourneyTests : PageTest
{
    private static string Origin => Environment.GetEnvironmentVariable("TICKORA_BASE_URL") ?? "http://localhost:5180";
    private static string Password => Environment.GetEnvironmentVariable("Demo__Password") ?? throw new InvalidOperationException("Demo__Password is required.");
    private async Task Login(IPage page, string email, string? origin = null)
    {
        await page.GotoAsync((origin ?? Origin) + "/account/login");
        await page.GetByLabel("Email address").FillAsync(email);
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in to your workspace" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "A clearer view of your support." })).ToBeVisibleAsync();
        await Expect(page.Locator(".metrics-grid")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true })).ToBeEnabledAsync();
    }
    [BrowserTheory]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task Customer_to_agent_conversation_preserves_private_notes(int width)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await Login(Page, "customer@tickora.example");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Create ticket", Exact = true }).ClickAsync();
        var subject = "Browser journey " + Guid.NewGuid().ToString("N")[..10];
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Create ticket" })).ToBeEnabledAsync();
        await Page.GetByLabel("Subject", new() { Exact = true }).FillAsync(subject);
        await Page.GetByLabel("Support queue").SelectOptionAsync(new SelectOptionValue { Label = "General Support" });
        await Page.GetByLabel("Category").SelectOptionAsync(new SelectOptionValue { Label = "Getting started" });
        await Page.GetByLabel("How can we help?").FillAsync("Please help our fictional team finish the browser workflow.");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create ticket" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = subject })).ToBeVisibleAsync();
        var ticketUrl = Page.Url;
        await Expect(Page.GetByLabel("Move conversation to")).ToHaveCountAsync(0);
        var publicFile = "customer-details-" + Guid.NewGuid().ToString("N") + ".txt";
        await Page.GetByLabel("Choose a file").SetInputFilesAsync(new FilePayload { Name = publicFile, MimeType = "text/plain", Buffer = System.Text.Encoding.UTF8.GetBytes("Customer attachment from the browser.") });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Upload attachment" }).ClickAsync();
        var publicDownload = Page.GetByRole(AriaRole.Link).Filter(new() { HasText = publicFile });
        await Expect(publicDownload).ToBeVisibleAsync();
        var downloaded = await Page.APIRequest.GetAsync(Origin + await publicDownload.GetAttributeAsync("href"));
        Assert.Equal("Customer attachment from the browser.", await downloaded.TextAsync());
        var secondaryOrigin = Environment.GetEnvironmentVariable("TICKORA_SECONDARY_BASE_URL");
        if (secondaryOrigin is not null)
        {
            await using var secondaryContext = await Browser.NewContextAsync();
            var secondaryPage = await secondaryContext.NewPageAsync();
            await Login(secondaryPage, "customer@tickora.example", secondaryOrigin);
            var fromOtherHost = await secondaryPage.APIRequest.GetAsync(secondaryOrigin + await publicDownload.GetAttributeAsync("href"));
            Assert.Equal(200, fromOtherHost.Status);
            Assert.Equal("Customer attachment from the browser.", await fromOtherHost.TextAsync());
        }
        await using var agentContext = await Browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = 900 } });
        var agent = await agentContext.NewPageAsync(); await Login(agent, "agent@tickora.example");
        await agent.GotoAsync(ticketUrl);
        await Expect(agent.GetByRole(AriaRole.Button, new() { Name = "Update assignment" })).ToBeEnabledAsync();
        await agent.GetByLabel("Assigned agent").SelectOptionAsync(new SelectOptionValue { Label = "Jamie Chen" });
        await agent.GetByRole(AriaRole.Button, new() { Name = "Update assignment" }).ClickAsync();
        await Expect(agent.Locator(".definition-list")).ToContainTextAsync("Jamie Chen");
        var privateNote = "Private investigation " + Guid.NewGuid().ToString("N");
        await agent.GetByLabel("Message", new() { Exact = true }).FillAsync(privateNote);
        await agent.GetByLabel("Internal note · visible to your team only").CheckAsync();
        await agent.GetByRole(AriaRole.Button, new() { Name = "Add note", Exact = true }).ClickAsync();
        await Expect(agent.Locator(".message").Filter(new() { HasText = privateNote })).ToHaveCountAsync(1);
        var privateFile = "private-investigation-" + Guid.NewGuid().ToString("N") + ".txt";
        await agent.GetByLabel("Choose a file").SetInputFilesAsync(new FilePayload { Name = privateFile, MimeType = "text/plain", Buffer = System.Text.Encoding.UTF8.GetBytes("Private team attachment from the browser.") });
        await agent.GetByLabel("Private team attachment").CheckAsync();
        await agent.GetByRole(AriaRole.Button, new() { Name = "Upload attachment" }).ClickAsync();
        await Expect(agent.GetByRole(AriaRole.Link).Filter(new() { HasText = privateFile })).ToBeVisibleAsync();
        await agent.GetByLabel("Internal note · visible to your team only").UncheckAsync();
        await agent.GetByLabel("Message", new() { Exact = true }).FillAsync("Here is the answer your team needs.");
        await agent.GetByRole(AriaRole.Button, new() { Name = "Send reply" }).ClickAsync();
        await Expect(agent.GetByText("Here is the answer your team needs.", new() { Exact = true })).ToBeVisibleAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Here is the answer your team needs.", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator("body")).Not.ToContainTextAsync(privateNote);
        await Expect(Page.Locator("body")).Not.ToContainTextAsync(privateFile);
        await agent.GetByLabel("Move conversation to").SelectOptionAsync("Resolved");
        await agent.GetByRole(AriaRole.Button, new() { Name = "Update status" }).ClickAsync();
        await Expect(agent.Locator(".detail-header .badge")).ToHaveTextAsync("Resolved");
        await Page.ReloadAsync(); await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "How did we do?" })).ToBeVisibleAsync();
        await Page.GetByLabel("Rating").SelectOptionAsync("5");
        await Page.GetByLabel("Anything else?", new() { Exact = true }).FillAsync("The support conversation was clear and helpful.");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send feedback", Exact = true }).ClickAsync();
        await Expect(Page.GetByText("Thank you. You rated this conversation 5 / 5.", new() { Exact = true })).ToBeVisibleAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Thank you. You rated this conversation 5 / 5.", new() { Exact = true })).ToBeVisibleAsync();
        await Page.GotoAsync(Origin + "/notifications"); await Expect(Page.GetByText("New reply on your ticket").First).ToBeVisibleAsync();
    }
    [BrowserFact]
    public async Task Knowledge_search_and_responsive_navigation_work()
    {
        await Login(Page, "customer@tickora.example");
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Toggle navigation", Exact = true }).CheckAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Knowledge base", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true })).ToBeEnabledAsync();
        await Page.GetByLabel("Search knowledge base").FillAsync("billing");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
        await Expect(Page.Locator(".article-card")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".article-card")).ToContainTextAsync("Understand invoices and billing");
        Assert.True(await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"));
        await Capture(Page, "knowledge-mobile.png");
    }
    [BrowserFact]
    public async Task Admin_dashboard_and_role_restrictions_work()
    {
        await Login(Page, "admin@tickora.example");
        await Page.SetViewportSizeAsync(1440, 1000);
        await Capture(Page, "dashboard-desktop.png");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Team & queues" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Add a support agent" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Checkbox).First).ToBeEnabledAsync();
        await using var customerContext = await Browser.NewContextAsync();
        var customer = await customerContext.NewPageAsync(); await Login(customer, "customer@tickora.example");
        var response = await customer.APIRequest.GetAsync(Origin + "/admin/team");
        Assert.Equal(403, response.Status);
    }

    [BrowserFact]
    public async Task Staff_can_publish_guides_and_edit_and_archive_saved_responses()
    {
        await Login(Page, "agent@tickora.example");
        await Page.GotoAsync(Origin + "/knowledge/manage");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save article", Exact = true })).ToBeEnabledAsync();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var title = "Browser guide " + suffix;
        await Page.GetByLabel("Title", new() { Exact = true }).FillAsync(title);
        await Page.GetByLabel("URL slug").FillAsync("browser-guide-" + suffix);
        await Page.GetByLabel("Category", new() { Exact = true }).FillAsync("Getting started");
        await Page.GetByLabel("Summary", new() { Exact = true }).FillAsync("A guide created through the real browser interface.");
        await Page.GetByLabel("Article content").FillAsync("Follow these steps to complete the fictional support setup.");
        await Page.GetByLabel("Published", new() { Exact = true }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save article", Exact = true }).ClickAsync();
        await Expect(Page.GetByText("Saved successfully.")).ToBeVisibleAsync();
        var manager = Page.Locator(".response-manager");
        await manager.GetByLabel("Response title").FillAsync("Browser response " + suffix);
        await manager.GetByLabel("Response text").FillAsync("An initial reusable answer.");
        await manager.GetByRole(AriaRole.Button, new() { Name = "Save response", Exact = true }).ClickAsync();
        var row = manager.Locator(".article-suggestion").Filter(new() { HasText = "Browser response " + suffix });
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await manager.GetByLabel("Response text").FillAsync("Updated and archived reusable answer.");
        await manager.GetByLabel("Active in composer").UncheckAsync();
        await manager.GetByRole(AriaRole.Button, new() { Name = "Save response", Exact = true }).ClickAsync();
        await Expect(row).ToContainTextAsync("Archived");
        await Page.GotoAsync(Origin + "/knowledge/browser-guide-" + suffix);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title, Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Related guides" })).ToBeVisibleAsync();
    }

    private static async Task Capture(IPage page, string name)
    {
        var directory = Environment.GetEnvironmentVariable("TICKORA_SCREENSHOTS");
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name), FullPage = true });
    }
}
