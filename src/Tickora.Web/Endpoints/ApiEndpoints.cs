using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Tickora.Application;
using Tickora.Domain;

namespace Tickora.Web.Endpoints;

public static class ApiEndpoints
{
    public static void MapTickoraApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireAuthorization().AddEndpointFilter(async (context, next) =>
        {
            if (context.HttpContext.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
                await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
            return await next(context);
        });
        api.MapGet("/antiforgery", (HttpContext h, IAntiforgery af) => Results.Ok(new { token = af.GetAndStoreTokens(h).RequestToken }));
        api.MapGet("/tickets", async ([AsParameters] ApiTicketFilter f, IHelpdeskQueries q, ICurrentUser u, CancellationToken ct) =>
            await q.TicketsAsync(await u.GetAsync(ct), new(f.Search, f.Status, f.Priority, f.Queue, f.Category, f.Agent, f.Mine ?? false, f.Unassigned ?? false, f.Overdue ?? false, f.From, f.To, f.Page ?? 1, f.PageSize ?? 12, f.Sort ?? "updated"), ct));
        api.MapPost("/tickets", async (CreateTicket input, TicketService s, CancellationToken ct) => { var id = await s.CreateAsync(input, ct); return Results.Created($"/api/v1/tickets/{id}", new { id }); });
        api.MapGet("/tickets/{id:guid}", async (Guid id, IHelpdeskQueries q, ICurrentUser u, CancellationToken ct) => await q.DetailAsync(await u.GetAsync(ct), id, ct));
        api.MapPatch("/tickets/{id:guid}", async (Guid id, UpdateTicket input, TicketService s, CancellationToken ct) => { await s.UpdateAsync(id, input, ct); return Results.NoContent(); });
        api.MapPost("/tickets/{id:guid}/reply", async (Guid id, ReplyTicket input, TicketService s, CancellationToken ct) => { await s.ReplyAsync(id, input, ct); return Results.NoContent(); });
        api.MapPost("/tickets/{id:guid}/transition", async (Guid id, TransitionTicket input, TicketService s, CancellationToken ct) => { await s.TransitionAsync(id, input, ct); return Results.NoContent(); });
        api.MapPost("/tickets/{id:guid}/assign", async (Guid id, AssignTicket input, TicketService s, CancellationToken ct) => { await s.AssignAsync(id, input, ct); return Results.NoContent(); });
        api.MapGet("/tickets/{id:guid}/activities", async (Guid id, IHelpdeskQueries q, ICurrentUser u, CancellationToken ct) => (await q.DetailAsync(await u.GetAsync(ct), id, ct)).Ticket.Activities);
        api.MapPost("/tickets/{id:guid}/feedback", async (Guid id, FeedbackInput input, TicketService s, CancellationToken ct) => { await s.FeedbackAsync(id, input.Rating, input.Comment, input.Version, ct); return Results.NoContent(); });
        api.MapGet("/notifications", async (int? page, int? pageSize, bool? unreadOnly, IHelpdeskQueries q, ICurrentUser u, CancellationToken ct) =>
            await q.NotificationsAsync((await u.GetAsync(ct)).Id, ct, page ?? 1, pageSize ?? 25, unreadOnly ?? false));
        api.MapPost("/notifications/{id:guid}/read", async (Guid id, IHelpdeskQueries q, ICurrentUser u, IClock clock, CancellationToken ct) => { await q.MarkReadAsync((await u.GetAsync(ct)).Id, id, clock.UtcNow, ct); return Results.NoContent(); });
        app.MapGet("/api/v1/knowledge/articles", async (string? search, IHelpdeskQueries q, CancellationToken ct) => await q.ArticlesAsync(search, false, null, ct));
        app.MapGet("/api/v1/knowledge/articles/{slug}", async (string slug, IHelpdeskQueries q, CancellationToken ct) =>
        {
            var article = await q.ArticleAsync(slug, ct);
            return article is null ? Results.NotFound() : Results.Ok(article);
        });
        api.MapGet("/reports/export", async (DateTimeOffset? from, DateTimeOffset? to, TicketStatus? status, Guid? queue, IHelpdeskQueries q, ICurrentUser u, CancellationToken ct) =>
        {
            var actor = await u.GetAsync(ct); if (!actor.Staff) throw TicketService.Forbidden();
            var csv = new StringBuilder("Number,Subject,Customer,Queue,Status,Priority\r\n");
            for (var page = 1; ; page++)
            {
                var result = await q.TicketsAsync(actor, new(Status: status, Queue: queue, From: from, To: to, Page: page, PageSize: 100), ct);
                foreach (var t in result.Items) csv.AppendLine(string.Join(',', new[] { $"TK-{t.Number}", t.Subject, t.Customer, t.Queue, t.Status.ToString(), t.Priority.ToString() }.Select(Csv)));
                if (page >= result.Pages) break;
            }
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "tickora-tickets.csv");
        }).RequireAuthorization("Staff");
    }
    public static string Csv(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
public sealed record FeedbackInput(int Rating, string Comment, string Version);
public sealed class ApiTicketFilter
{
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public Guid? Queue { get; set; }
    public Guid? Category { get; set; }
    public string? Agent { get; set; }
    public bool? Mine { get; set; }
    public bool? Unassigned { get; set; }
    public bool? Overdue { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}
