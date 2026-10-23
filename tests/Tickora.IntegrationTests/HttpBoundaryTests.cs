using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tickora.IntegrationTests;

public sealed class HttpBoundaryTests : IDisposable
{
    private readonly WebApplicationFactory<Program> app = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b => b.UseEnvironment("Development").UseSetting("Sla:MonitorEnabled", "false"));
    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    [InlineData("/account/forgot")]
    [InlineData("/account/reset")]
    public async Task Account_form_renders_with_antiforgery(string path)
    {
        using var client = app.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains("method=\"post\"", html);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }
    [Fact]
    public async Task Api_authentication_is_enforced_before_reading_data()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/tickets")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/notifications")).StatusCode);
    }
    [Fact]
    public async Task Account_posts_require_antiforgery()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var result = await client.PostAsync("/account/actions/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = "fake@example.test", ["Password"] = "invalid" }));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
    [Fact]
    public async Task Liveness_does_not_depend_on_database_availability()
    {
        using var client = app.CreateClient(); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }
    public void Dispose() => app.Dispose();
}
