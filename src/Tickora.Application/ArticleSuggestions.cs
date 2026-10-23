using Tickora.Domain;

namespace Tickora.Application;

public static class ArticleSuggestions
{
    public static IReadOnlyList<KnowledgeArticle> Rank(IEnumerable<KnowledgeArticle> articles, string subject, string? category)
    {
        var terms = subject.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 3).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
        return articles.Where(a => a.Published)
            .OrderByDescending(a => (string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase) ? 3 : 0) +
                terms.Count(t => a.Title.Contains(t, StringComparison.OrdinalIgnoreCase) || a.Summary.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(a => a.Featured).ThenBy(a => a.Title).Take(4).ToList();
    }
}
