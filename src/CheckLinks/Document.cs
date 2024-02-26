namespace CheckLinks;

using System.Linq;
using HtmlAgilityPack;
using Markdig;

public class Document(string url, string path)
{
    public string Url { get; } = url;
    public string GitHubPath { get; } = string.Join("/", url.Split('/').Skip(7));
    public DocumentLink[] Links { get; private set; }

    public void ExtractLinks()
    {
        var markdown = File.ReadAllText(path);
        var html = Markdown.ToHtml(markdown);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode.SelectNodes("//a[not(contains(@class, 'anchor')) and @href and not(starts-with(@href, '#footnote')) and not(starts-with(@href, 'mailto:'))]")
            ?.OfType<HtmlNode>() ?? Enumerable.Empty<HtmlNode>();

        Links = links
            .Select(link => new DocumentLink(Url, link))
            .ToArray();
    }
}
