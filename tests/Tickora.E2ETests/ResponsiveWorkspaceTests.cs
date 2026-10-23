using Microsoft.Playwright;

namespace Tickora.E2ETests;

public sealed partial class SupportJourneyTests
{
    [BrowserFact]
    public async Task Ticket_search_and_pagination_work_on_mobile()
    {
        await Login(Page, "admin@tickora.example");
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(Origin + "/tickets");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Reset filters", Exact = true })).ToBeEnabledAsync();
        var ticketText = await Page.Locator(".ticket-subject").First.InnerTextAsync();
        var number = System.Text.RegularExpressions.Regex.Match(ticketText, @"TK-\d+").Value;
        Assert.NotEmpty(number);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(Page.Locator(".page-number")).ToContainTextAsync("2 /");
        await Page.GetByLabel("Search tickets").FillAsync(number);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
        await Expect(Page.Locator(".ticket-table tbody tr")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".ticket-table tbody")).ToContainTextAsync(number);
        await Expect(Page.Locator(".page-number")).ToHaveTextAsync("1 / 1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Reset filters", Exact = true }).ClickAsync();
        await Expect(Page.Locator(".ticket-table tbody tr")).ToHaveCountAsync(12);
    }

    [BrowserFact]
    public async Task Workspace_pages_fit_desktop_tablet_and_small_mobile_screens()
    {
        await Login(Page, "admin@tickora.example");
        string[] routes = ["/", "/tickets", "/tickets/new", "/customers", "/knowledge", "/knowledge/manage", "/reports", "/notifications", "/admin/team", "/admin/sla", "/settings"];
        var failures = new List<string>();
        var errors = new List<string>();
        Page.PageError += (_, error) => errors.Add(error);
        foreach (var width in new[] { 320, 390, 768, 1440 })
        {
            await Page.SetViewportSizeAsync(width, 900);
            foreach (var route in routes)
            {
                await Page.GotoAsync(Origin + route);
                await Expect(Page.Locator("main h1")).ToBeVisibleAsync();
                await Expect(Page.Locator(".skeleton")).ToHaveCountAsync(0);
                await Expect(Page.Locator(".alert.error")).ToHaveCountAsync(0);
                var overflow = await Page.EvaluateAsync<int>("document.documentElement.scrollWidth - window.innerWidth");
                if (overflow > 1) failures.Add($"{width}px {route}: horizontal overflow {overflow}px");
                Assert.Equal("dark", await Page.EvaluateAsync<string>("getComputedStyle(document.documentElement).colorScheme"));
                if (width is 390 or 1440 && route is "/" or "/tickets/new" or "/reports")
                    await Capture(Page, $"review-{width}-{(route == "/" ? "overview" : route.Trim('/').Replace('/', '-'))}.png");
            }
        }
        Assert.Empty(errors);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [BrowserFact]
    public async Task Mobile_navigation_supports_keyboard_backdrop_and_hidden_links()
    {
        await Login(Page, "admin@tickora.example");
        await Page.SetViewportSizeAsync(390, 844);
        var sidebar = Page.Locator(".sidebar");
        var toggle = Page.GetByRole(AriaRole.Checkbox, new() { Name = "Toggle navigation", Exact = true });
        await Expect(sidebar).ToBeHiddenAsync();
        await toggle.PressAsync("Space");
        await Expect(sidebar).ToBeVisibleAsync();
        await Page.Locator(".sidebar-backdrop").ClickAsync(new() { Position = new() { X = 350, Y = 100 } });
        await Expect(sidebar).ToBeHiddenAsync();
        await toggle.PressAsync("Space");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Reports", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Make the next conversation better." })).ToBeVisibleAsync();
        await Expect(sidebar).ToBeHiddenAsync();
    }

    [BrowserFact]
    public async Task Ticket_details_keep_selected_category_and_fit_mobile_conversations()
    {
        await Login(Page, "admin@tickora.example");
        await Page.GotoAsync(Origin + "/tickets");
        await Expect(Page.Locator(".ticket-table tbody tr").First).ToBeVisibleAsync();
        await Page.Locator(".ticket-subject").First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Ticket details", Exact = true })).ToBeVisibleAsync();
        var category = Page.GetByLabel("Category");
        await Expect(category).Not.ToHaveValueAsync("");
        var selected = await category.Locator("option:checked").TextContentAsync();
        var labels = await Page.Locator(".definition-list dd").AllTextContentsAsync();
        Assert.Equal(labels[2], selected);
        foreach (var width in new[] { 320, 390, 768, 1440 })
        {
            await Page.SetViewportSizeAsync(width, 900);
            Assert.True(await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"), $"Ticket detail overflows at {width}px");
        }
    }
}
