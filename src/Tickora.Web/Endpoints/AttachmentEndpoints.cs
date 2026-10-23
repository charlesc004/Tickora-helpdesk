using Tickora.Application;
using Tickora.Domain;

namespace Tickora.Web.Endpoints;

public static class AttachmentEndpoints
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public static void MapAttachments(this WebApplication app)
    {
        app.MapPost("/api/v1/tickets/{id:guid}/attachments", async (Guid id, HttpContext h, IHelpdeskQueries queries, ICurrentUser user, IFileStorage storage, TicketService service, CancellationToken ct) =>
        {
            var actor = await user.GetAsync(ct);
            await queries.DetailAsync(actor, id, ct); // Check ownership before reading uploaded content.
            await h.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(h);
            var form = await h.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? throw TicketService.Invalid("Choose a file.");
            var internalFile = bool.TryParse(form["internal"], out var p) && p;
            if (internalFile && !actor.Staff) throw TicketService.Forbidden();
            if (file.Length is < 1 or > MaxBytes) throw TicketService.Invalid("Files must be between 1 byte and 5 MB.");
            var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
            if (name.Length > 180 || name.Any(char.IsControl)) throw TicketService.Invalid("Invalid filename.");
            var extension = Path.GetExtension(name).ToLowerInvariant();
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var type = ValidateFile(extension, bytes);
            buffer.Position = 0;
            var key = await storage.WriteAsync(buffer, ct);
            try
            {
                await service.AddAttachmentAsync(id, new TicketAttachment { FileName = name, StorageKey = key, ContentType = type, Length = file.Length, Internal = internalFile }, form["version"].ToString(), ct);
            }
            catch { await storage.DeleteAsync(key, CancellationToken.None); throw; }
            if (form["redirect"] == "true") return Results.LocalRedirect($"/tickets/{id}");
            return Results.Created($"/api/v1/tickets/{id}/attachments", new { name });
        }).RequireAuthorization();
        app.MapGet("/api/v1/tickets/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId, HttpContext h, IHelpdeskQueries queries, ICurrentUser user, IFileStorage storage, CancellationToken ct) =>
        {
            var detail = await queries.DetailAsync(await user.GetAsync(ct), id, ct);
            var file = detail.Ticket.Attachments.SingleOrDefault(x => x.Id == attachmentId) ?? throw new WorkflowException("not_found", "Attachment not found.", 404);
            h.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(await storage.ReadAsync(file.StorageKey, ct), file.ContentType, file.FileName);
        }).RequireAuthorization();
    }
    public static string ValidateFile(string extension, byte[] bytes)
    {
        if (extension == ".png" && bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (extension is ".jpg" or ".jpeg" && bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 })) return "image/jpeg";
        if (extension == ".pdf" && bytes.AsSpan().StartsWith("%PDF-"u8)) return "application/pdf";
        if (extension == ".txt")
        {
            try { var text = new System.Text.UTF8Encoding(false, true).GetString(bytes); if (!text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'))) return "text/plain"; }
            catch (System.Text.DecoderFallbackException) { }
        }
        throw TicketService.Invalid("Only valid PNG, JPEG, PDF, and UTF-8 text files are allowed.");
    }
}
