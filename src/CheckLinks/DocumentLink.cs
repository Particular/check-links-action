namespace CheckLinks;

using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

public partial class DocumentLink
{
    public Uri Uri { get; }
    public string Href { get; }
    public string Text { get; }
    public bool IsValid { get; private set; }
    public string SourceBranch { get; private set; }
    public string Reason { get; private set; }
    public string SuggestedReplacement { get; private set; }

    public DocumentLink(string docUrl, HtmlNode node)
    {
        Href = node.Attributes["Href"].Value;
        Text = node.InnerText;

        if (Uri.TryCreate(Href, UriKind.Absolute, out var uri))
        {
            Uri = uri;
        }
        else if (Uri.TryCreate(Href, UriKind.Relative, out var relative))
        {
            var baseUri = new Uri(docUrl);
            if (Href.StartsWith("/") && baseUri.Host == "github.com" && baseUri.AbsolutePath.StartsWith("/Particular", StringComparison.OrdinalIgnoreCase))
            {
                var threeSegments = baseUri.AbsolutePath.Split('/').Take(5);
                var newRelativePath = string.Join("/", threeSegments) + relative;
                relative = new Uri(newRelativePath, UriKind.Relative);
            }
            var newUri = new Uri(baseUri, relative);
            Uri = newUri;
        }

        SourceBranch = GetRepoAndBranchRegex().Match(docUrl).Groups["Branch"].Value;
    }

    static readonly HashSet<string> ignoredHrefs = new(["LINK", "TODO", "URL"], StringComparer.OrdinalIgnoreCase);

    public void Validate()
    {
        if (ignoredHrefs.Contains(Href))
        {
            IsValid = true;
            return;
        }

        if (ParticularGitHubRegex().IsMatch(Uri.AbsoluteUri))
        {
            if (GitHubFS.IsValidGitHubUrl(Uri))
            {
                IsValid = true;
                return;
            }
        }
        else
        {
            IsValid = true;
        }
    }

    public void Suggest()
    {
        if (!ParticularGitHubRegex().IsMatch(Uri.AbsoluteUri))
        {
            return;
        }

        var match = GetRepoAndBranchRegex().Match(Uri.AbsoluteUri);
        if (match.Success)
        {
            var repoName = match.Groups["Repo"].Value;
            var referenceName = match.Groups["Branch"].Value;
            if (referenceName is not "main" and not "master")
            {
                var defaultBranchForRepo = GitHubFS.GetDefaultBranchName(repoName);
                var changeBranchUrl = Uri.AbsoluteUri.Replace($"/{referenceName}/", $"/{defaultBranchForRepo}/");
                if (GitHubFS.IsValidGitHubUrl(new Uri(changeBranchUrl)))
                {
                    Reason = "Uses non-default branch";
                    SuggestedReplacement = changeBranchUrl;
                    return;
                }
            }
        }
    }

    [GeneratedRegex(@"^https://github.com/(Particular|orgs/Particular|organizations/Particular)(/.*|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ParticularGitHubRegex();

    [GeneratedRegex(@"https://github.com/Particular/(?<Repo>[^/]+)/(tree|blob)/(?<Branch>[^/]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GetRepoAndBranchRegex();
}
