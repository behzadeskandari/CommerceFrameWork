using System.Text.RegularExpressions;

namespace Commerce.Notifications.Application.Templates;

public static partial class NotificationTemplateRenderer
{
    private static readonly Regex TokenPattern = TokenRegex();

    public static string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return TokenPattern.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
}

public static class NotificationTemplateSelector
{
    public static IReadOnlyList<Commerce.Notifications.Domain.Entities.NotificationTemplate> Select(
        IReadOnlyList<Commerce.Notifications.Domain.Entities.NotificationTemplate> candidates,
        int? storeId,
        int? languageId)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var grouped = candidates
            .GroupBy(x => x.Channel)
            .Select(group => SelectBest(group.ToList(), storeId, languageId))
            .Where(x => x is not null)
            .Cast<Commerce.Notifications.Domain.Entities.NotificationTemplate>()
            .ToList();

        return grouped;
    }

    private static Commerce.Notifications.Domain.Entities.NotificationTemplate? SelectBest(
        IReadOnlyList<Commerce.Notifications.Domain.Entities.NotificationTemplate> templates,
        int? storeId,
        int? languageId)
    {
        return templates
            .OrderByDescending(ScoreTemplate)
            .FirstOrDefault();

        int ScoreTemplate(Commerce.Notifications.Domain.Entities.NotificationTemplate template)
        {
            var score = 0;
            if (storeId.HasValue && template.StoreId == storeId)
            {
                score += 4;
            }
            else if (!template.StoreId.HasValue)
            {
                score += 1;
            }
            else
            {
                return -1;
            }

            if (languageId.HasValue && template.LanguageId == languageId)
            {
                score += 2;
            }
            else if (!template.LanguageId.HasValue)
            {
                score += 1;
            }

            return score;
        }
    }
}
