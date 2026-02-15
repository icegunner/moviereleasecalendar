using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public partial class ScraperService
    {
        protected async Task<string> TryFetchHtmlForYearAsync(int year, CancellationToken cancellationToken = default)
        {
            var url = $"https://www.firstshowing.net/schedule{year}";
            _logger.LogInformation($"Fetching: {url}");

            try
            {
                return await _client.GetStringAsync(url, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download year {year}");
                return null;
            }
        }

        protected List<HtmlNode> CollectAnchorGroup(ref HtmlNode node)
        {
            var group = new List<HtmlNode>();
            var checkNode = node;
            var ignoreNode = false;

            while (checkNode != null && checkNode.Name != "br")
            {
                if (checkNode.InnerHtml.ToLower().Contains("expands") || checkNode.InnerHtml.ToLower().Contains("re-release"))
                {
                    ignoreNode = true;
                }
                checkNode = checkNode.NextSibling;
            }

            while (node != null && node.Name != "br")
            {
                if (node.Name == "a" && !ignoreNode)
                    group.Add(node);

                node = node.NextSibling;
            }

            if (node?.Name == "br")
                node = node.NextSibling;

            return group;
        }

        protected bool IsStruckThrough(HtmlNode anchor)
        {
            var strong = anchor.SelectSingleNode(".//strong");
            return strong == null || strong.SelectSingleNode(".//s") != null;
        }

        protected (string RawTitle, string CleanTitle, string NormalizedTitle) ExtractTitles(HtmlNode anchor)
        {
            var rawTitle = anchor.InnerText.Trim();
            var cleanTitle = Regex.Replace(rawTitle, @"\s*\[.*?\]\s*$", "");
            var normalizedTitle = NormalizeTitle(cleanTitle);
            return (rawTitle, cleanTitle, normalizedTitle);
        }

        protected string NormalizeTitle(string title)
        {
            string normalized = title.Normalize(NormalizationForm.FormD); // Normalize Unicode to decompose accents
            // Remove diacritics
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            string withoutDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
            string cleaned = Regex.Replace(withoutDiacritics, @"[^a-zA-Z0-9_]", ""); // Remove all non-alphanumeric and non-underscore characters
            return cleaned.ToLower();
        }

        protected List<string> GetAlternativeTitles(string title)
        {
            var titles = new List<string> { title };

            // Remove anything after colon or dash
            var baseTitle = Regex.Replace(title, @"\s*[:\-]\s*.*$", "");
            if (baseTitle != title) titles.Add(baseTitle);

            // Remove possessives, HTML entities, and after colon/dash
            var superCleanTitle = Regex.Replace(title, @"(\b[\p{L}\p{M}\p{N}\.]+(?:\s+[\p{L}\p{M}\p{N}\.]+)*'s?\s+)|(&#\d+;)|(\s*[:\-]\s*.*$)", "").TrimEnd();
            if (superCleanTitle != baseTitle && superCleanTitle != title) titles.Add(superCleanTitle);

            return titles.Distinct().ToList();
        }

        protected string NormalizeLink(HtmlNode anchor)
        {
            var link = anchor.GetAttributeValue("href", string.Empty).Trim();
            return link.StartsWith("//") ? $"https:{link}" : link;
        }

        protected DateTime? GetDateFromTag(HtmlNode tag, int year)
        {
            var strong = tag.SelectSingleNode(".//strong");
            if (strong == null) return null;

            var text = strong.InnerText.Trim();
            try
            {
                return DateTime.ParseExact($"{text}, {year}", "MMMM d, yyyy", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                _logger.LogDebug($"Failed to parse date: {text}");
                return null;
            }
        }
    }
}
