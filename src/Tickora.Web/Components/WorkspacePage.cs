using Microsoft.AspNetCore.Components;
using Tickora.Application;

namespace Tickora.Web.Components;

public abstract class WorkspacePage : ComponentBase, IDisposable
{
    [Inject] protected ICurrentUser CurrentUser { get; set; } = default!;
    [Inject] protected IHelpdeskQueries Queries { get; set; } = default!;
    [Inject] protected ILogger<WorkspacePage> Logger { get; set; } = default!;
    protected Actor? Actor { get; set; }
    protected string? Error { get; set; }
    protected bool Loading { get; set; } = true;
    protected bool Busy { get; set; }
    private readonly CancellationTokenSource Lifetime = new();
    protected CancellationToken LifetimeToken { get; }
    protected WorkspacePage() => LifetimeToken = Lifetime.Token;
    protected async Task<bool> Run(Func<Task> action)
    {
        if (Busy || LifetimeToken.IsCancellationRequested) return false;
        Busy = true; Error = null;
        try { Actor ??= await CurrentUser.GetAsync(LifetimeToken); await action(); return true; }
        catch (WorkflowException e) { Error = e.Message; return false; }
        catch (OperationCanceledException) when (Lifetime.IsCancellationRequested) { return false; }
        catch (Exception e) { Logger.LogError(e, "Workspace operation failed"); Error = "We couldn't load or save this information. Check the database connection and try again."; return false; }
        finally { Busy = false; Loading = false; }
    }
    public void Dispose() { if (LifetimeToken.IsCancellationRequested) return; Lifetime.Cancel(); Lifetime.Dispose(); GC.SuppressFinalize(this); }
}
