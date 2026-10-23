using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tickora.Domain;

namespace Tickora.Infrastructure.Persistence;

public sealed class DemoSeeder(ApplicationDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
{
    public async Task SeedAsync(string password, CancellationToken ct = default)
    {
        foreach (var name in new[] { "Admin", "Agent", "Customer" })
            if (!await roles.RoleExistsAsync(name)) Check(await roles.CreateAsync(new IdentityRole(name)));
        async Task<ApplicationUser> Account(string email, string name, string role)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, DisplayName = name, EmailConfirmed = true };
                Check(await users.CreateAsync(user, password)); Check(await users.AddToRoleAsync(user, role));
            }
            return user;
        }
        var admin = await Account("admin@tickora.example", "Alex Morgan", "Admin");
        string[] agentNames = ["Jamie Chen", "Sam Rivera", "Taylor Brooks", "Jordan Ellis", "Casey Patel"];
        var agents = new List<ApplicationUser>();
        for (var i = 0; i < 5; i++) agents.Add(await Account(i == 0 ? "agent@tickora.example" : $"agent{i + 1}@tickora.example", agentNames[i], "Agent"));
        string[] firstNames = ["Avery", "Morgan", "Riley", "Quinn", "Cameron", "Parker", "Reese", "Skyler", "Rowan", "Drew"];
        var customers = new List<ApplicationUser>();
        for (var i = 0; i < 30; i++) customers.Add(await Account(i == 0 ? "customer@tickora.example" : $"customer{i + 1}@tickora.example", $"{firstNames[i % 10]} {new[] { "Hayes", "Bennett", "Foster" }[i / 10]}", "Customer"));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var name in new[] { "General Support", "Technical Support", "Billing", "Account Assistance" })
            if (!await db.Queues.AnyAsync(x => x.Name == name, ct)) db.Queues.Add(new SupportQueue { Name = name });
        foreach (var name in new[] { "Getting started", "Account access", "Billing & plans", "Integrations", "Product questions" })
            if (!await db.Categories.AnyAsync(x => x.Name == name, ct)) db.Categories.Add(new TicketCategory { Name = name });
        var calendar = JsonSerializer.Serialize(new BusinessCalendar());
        int[] response = [1440, 480, 120, 30], resolution = [4320, 1920, 480, 240];
        foreach (var p in Enum.GetValues<TicketPriority>())
            if (!await db.SlaPolicies.AnyAsync(x => x.Priority == p, ct)) db.SlaPolicies.Add(new SlaPolicy
            {
                Priority = p,
                FirstResponseMinutes = response[(int)p],
                ResolutionMinutes = resolution[(int)p],
                CalendarJson = calendar
            });
        await db.SaveChangesAsync(ct);
        var queues = await db.Queues.OrderBy(x => x.Name).ToListAsync(ct);
        var cats = await db.Categories.OrderBy(x => x.Name).ToListAsync(ct);
        foreach (var agent in agents.Append(admin)) foreach (var queue in queues)
            if (!await db.Memberships.AnyAsync(x => x.AgentId == agent.Id && x.QueueId == queue.Id, ct)) db.Memberships.Add(new QueueMembership { AgentId = agent.Id, QueueId = queue.Id });
        if (!await db.Tickets.AnyAsync(ct))
        {
            string[] subjects = ["Unable to connect the Slack integration", "Invoice total needs clarification", "Help setting up our new workspace", "Password reset email hasn't arrived", "Export is missing recent records", "Update the billing contact", "API requests returning a timeout", "Add a teammate to our account", "Dashboard data is slow to refresh", "Question about annual plan renewal", "Webhook delivery is delayed", "Move our workspace to a new plan"];
            for (var i = 0; i < 108; i++)
            {
                var created = DateTimeOffset.UtcNow.AddDays(-5 - i % 25).AddHours(-(i % 6));
                var priority = (TicketPriority)(i % 4); var status = (TicketStatus)(i % 5);
                var cal = new BusinessCalendar();
                var t = new Ticket
                {
                    Subject = subjects[i % subjects.Length],
                    Description = "We are testing this workflow in our fictional Northstar Studio workspace. Could you help us investigate and recommend the next steps?",
                    CustomerId = customers[i % 30].Id,
                    AssignedAgentId = i % 7 == 0 && (status is TicketStatus.Open or TicketStatus.InProgress) ? null : agents[i % 5].Id,
                    QueueId = queues[i % 4].Id,
                    CategoryId = cats[i % 5].Id,
                    Priority = priority,
                    Status = status,
                    CreatedAt = created,
                    UpdatedAt = created.AddHours(1),
                    SubmissionId = Guid.NewGuid(),
                    CalendarSnapshot = calendar,
                    FirstResponseMinutes = response[(int)priority],
                    ResolutionMinutes = resolution[(int)priority],
                    PauseWhileWaiting = true,
                    FirstResponseDue = SlaCalculationPolicy.AddBusinessTime(created, TimeSpan.FromMinutes(response[(int)priority]), cal),
                    ResolutionDue = SlaCalculationPolicy.AddBusinessTime(created, TimeSpan.FromMinutes(resolution[(int)priority]), cal)
                };
                t.Activities.Add(new TicketActivity { TicketId = t.Id, ActorId = t.CustomerId, Description = "Ticket created", CreatedAt = created });
                if (t.AssignedAgentId is not null && status != TicketStatus.Open)
                {
                    var replyAt = SlaCalculationPolicy.AddBusinessTime(created, TimeSpan.FromMinutes(i % 8 == 0 ? response[(int)priority] + 60 : Math.Max(1, response[(int)priority] / 2)), cal);
                    t.FirstResponseAt = replyAt; t.UpdatedAt = replyAt;
                    t.Messages.Add(new TicketMessage
                    {
                        TicketId = t.Id,
                        AuthorId = t.AssignedAgentId,
                        AuthorName = agents[i % 5].DisplayName,
                        Body = "Thanks for the details. I'm investigating this with our team and will share an update shortly.",
                        CreatedAt = replyAt,
                        SubmissionId = Guid.NewGuid()
                    });
                    t.Messages.Add(new TicketMessage
                    {
                        TicketId = t.Id,
                        AuthorId = t.AssignedAgentId,
                        AuthorName = agents[i % 5].DisplayName,
                        Body = "Internal: review the workspace configuration before escalating.",
                        Visibility = MessageVisibility.Internal,
                        CreatedAt = replyAt.AddMinutes(1),
                        SubmissionId = Guid.NewGuid()
                    });
                }
                if (status is TicketStatus.Resolved or TicketStatus.Closed)
                {
                    t.FirstResponseAt ??= SlaCalculationPolicy.AddBusinessTime(created, TimeSpan.FromMinutes(10), cal);
                    t.ResolvedAt = SlaCalculationPolicy.AddBusinessTime(created, TimeSpan.FromMinutes(i % 9 == 0 ? resolution[(int)priority] + 30 : 90), cal);
                    // Historical records must stay in the past and responses must precede resolution.
                    if (t.ResolvedAt < t.FirstResponseAt) t.ResolvedAt = t.FirstResponseAt.Value.AddMinutes(15);
                    if (t.ResolvedAt > DateTimeOffset.UtcNow) { t.Status = TicketStatus.InProgress; t.ResolvedAt = null; }
                    else
                    {
                        t.UpdatedAt = t.ResolvedAt.Value;
                        if (status == TicketStatus.Closed) { t.ClosedAt = t.ResolvedAt.Value.AddMinutes(30); t.UpdatedAt = t.ClosedAt.Value; }
                        db.Feedback.Add(new CustomerFeedback { TicketId = t.Id, Rating = 3 + i % 3, Comment = "Thank you for helping our team.", CreatedAt = t.UpdatedAt.AddMinutes(1) });
                    }
                }
                if (status == TicketStatus.WaitingForCustomer) t.Pauses.Add(new SlaPause { TicketId = t.Id, StartedAt = t.UpdatedAt });
                db.Tickets.Add(t);
                db.Notifications.Add(new Notification { TicketId = t.Id, RecipientId = t.CustomerId, Title = "Your support ticket is being tracked", EventKey = $"seed:{t.Id}", CreatedAt = t.CreatedAt });
            }
        }
        if (!await db.Articles.AnyAsync(ct))
        {
            string[] titles = ["Getting started with your workspace", "Connect your first integration", "Understand invoices and billing", "Manage team roles and permissions", "Reset your account password", "Troubleshoot webhook delivery"];
            foreach (var title in titles) db.Articles.Add(new KnowledgeArticle
            {
                Title = title,
                Slug = title.ToLowerInvariant().Replace(' ', '-'),
                Summary = "A practical guide to help your team get back to work.",
                Category = title.Contains("billing") ? "Billing & plans" : "Getting started",
                Body = $"{title}\n\nBefore you begin, sign in to your workspace and open Settings.\n\n1. Check that you have the appropriate permissions.\n2. Review the current configuration and save any changes.\n3. Try the action again in a new session.\n\nIf the problem continues, create a support ticket with the steps you followed and any error message. Never include passwords or access tokens.",
                AuthorId = admin.Id,
                Published = true,
                Featured = true,
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            });
        }
        if (!await db.Responses.AnyAsync(ct)) db.Responses.AddRange(new CannedResponse { Title = "First response", Body = "Thanks for reaching out. I'm reviewing your request and will get back to you with next steps." }, new CannedResponse { Title = "Request more details", Body = "Could you share the steps to reproduce this issue and the error message you see? Please avoid including sensitive information." });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
