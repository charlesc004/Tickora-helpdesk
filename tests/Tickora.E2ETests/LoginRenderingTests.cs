using Microsoft.Playwright;

namespace Tickora.E2ETests;

public sealed class SmokeFactAttribute : FactAttribute
{
    public SmokeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TICKORA_SMOKE") != "1") Skip = "Requires a running application. Set TICKORA_SMOKE=1 for anonymous browser rendering checks.";
    }
}
public sealed class LoginRenderingTests
{
    [SmokeFact]
    public async Task Login_forms_render_at_desktop_and_mobile_widths()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Channel = Environment.GetEnvironmentVariable("TICKORA_BROWSER_CHANNEL") });
        var page = await browser.NewPageAsync();
        var origin = Environment.GetEnvironmentVariable("TICKORA_BASE_URL") ?? "http://localhost:5180";
        foreach (var width in new[] { 1440, 390 })
        {
            await page.SetViewportSizeAsync(width, 900);
            var response = await page.GotoAsync(origin + "/account/login");
            Assert.Equal(200, response!.Status);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back." })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByLabel("Email address")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".alert.error")).ToHaveCountAsync(0);
            await page.GetByLabel("Email address").FocusAsync();
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"));
            var output = Environment.GetEnvironmentVariable("TICKORA_SCREENSHOTS");
            if (output is not null) { Directory.CreateDirectory(output); await page.ScreenshotAsync(new() { Path = Path.Combine(output, $"login-{width}.png"), FullPage = true }); }
        }
        await page.GetByRole(AriaRole.Link, new() { Name = "Create an account", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByLabel("Your name")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Back to sign in", Exact = false }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Forgot password?" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Send reset instructions" })).ToBeVisibleAsync();
    }
}
