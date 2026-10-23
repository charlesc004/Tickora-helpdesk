using Tickora.Domain;

namespace Tickora.Application;

public sealed record TeamMember(string Id, string Name, string Email, bool Active, string Role, IReadOnlyList<Guid> Queues);
public interface IAdministration
{
    Task<IReadOnlyList<TeamMember>> TeamAsync(CancellationToken ct = default);
    Task CreateAgentAsync(string name, string email, string password, CancellationToken ct = default);
    Task SetMembershipAsync(string userId, Guid queueId, bool member, CancellationToken ct = default);
    Task SetActiveAsync(string userId, bool active, CancellationToken ct = default);
    Task SetRoleAsync(string userId, string role, CancellationToken ct = default);
    Task<IReadOnlyList<SupportQueue>> QueuesAsync(CancellationToken ct = default);
    Task SaveQueueAsync(Guid? id, string name, bool active, string? version, CancellationToken ct = default);
    Task<IReadOnlyList<SlaPolicy>> PoliciesAsync(CancellationToken ct = default);
    Task SavePolicyAsync(SlaPolicy input, CancellationToken ct = default);
    Task SaveArticleAsync(KnowledgeArticle input, CancellationToken ct = default);
    Task CreateResponseAsync(string title, string body, bool active = true, CancellationToken ct = default);
    Task<IReadOnlyList<CannedResponse>> ResponsesAsync(CancellationToken ct = default);
    Task UpdateResponseAsync(CannedResponse input, CancellationToken ct = default);
    Task SaveCategoryAsync(Guid? id, string name, bool active, CancellationToken ct = default, string? version = null);
    Task<IReadOnlyList<TicketCategory>> CategoriesAsync(CancellationToken ct = default);
}
