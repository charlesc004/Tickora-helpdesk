using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tickora.Domain;
using Tickora.Infrastructure.Persistence;

namespace Tickora.IntegrationTests;

public sealed class EntityMappingTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=localhost;Database=MappingOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void Appending_a_message_to_an_existing_ticket_tracks_an_insert()
    {
        using var db = Context(); var ticket = new Ticket(); db.Attach(ticket);
        var message = new TicketMessage { TicketId = ticket.Id, Body = "A new reply" };
        ticket.Messages.Add(message); db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Added, db.Entry(message).State);
    }
    [Fact]
    public void Ticket_concurrency_uses_database_generated_rowversion()
    {
        using var db = Context(); var property = db.Model.FindEntityType(typeof(Ticket))!.FindProperty(nameof(Ticket.RowVersion))!;
        Assert.True(property.IsConcurrencyToken); Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }
    [Fact]
    public void Notification_keys_are_unique_per_recipient_and_event()
    {
        using var db = Context(); Assert.Contains(db.Model.FindEntityType(typeof(Notification))!.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual(new[] { "RecipientId", "EventKey" }));
    }
}
