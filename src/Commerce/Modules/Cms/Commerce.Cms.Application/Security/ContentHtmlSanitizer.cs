namespace Commerce.Cms.Application.Security;

public interface IContentHtmlSanitizer
{
    string Sanitize(string? html);
}

public sealed class ContentHtmlSanitizer : IContentHtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags =
    [
        "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a", "h1", "h2", "h3", "h4", "h5", "h6",
        "img", "blockquote", "div", "span", "table", "thead", "tbody", "tr", "th", "td", "hr", "pre", "code"
    ];

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var result = html;
        result = RemoveBlockedPatterns(result);
        result = StripDisallowedTags(result);
        result = StripEventHandlers(result);
        return result;
    }

    private static string RemoveBlockedPatterns(string html)
    {
        var patterns = new[]
        {
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
            @"<iframe\b[^>]*>.*?<\/iframe>",
            @"javascript\s*:",
            @"data\s*:\s*text/html"
        };

        foreach (var pattern in patterns)
        {
            html = System.Text.RegularExpressions.Regex.Replace(html, pattern, string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        return html;
    }

    private static string StripDisallowedTags(string html) =>
        System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<\/?([a-zA-Z0-9]+)(\s[^>]*)?>",
            match =>
            {
                var tag = match.Groups[1].Value.ToLowerInvariant();
                return AllowedTags.Contains(tag) ? match.Value : string.Empty;
            },
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static string StripEventHandlers(string html) =>
        System.Text.RegularExpressions.Regex.Replace(
            html,
            @"\s(on\w+)\s*=",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
