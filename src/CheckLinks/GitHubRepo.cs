namespace CheckLinks;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using LibGit2Sharp;

class GitHubRepo(string repoName)
{
    readonly string repoPath = Path.Combine(AppConfig.CheckoutDirectory, repoName);
    readonly string cloneUrl = $"https://github.com/Particular/{repoName}.git";

    Repository repository;

    public string DefaultBranch { get; private set; }

    public void Clone()
    {
        if (repository is not null)
        {
            return;
        }

        Directory.CreateDirectory(repoPath);

        Repository.Init(repoPath);
        var repo = new Repository(repoPath);

        if (Directory.GetFiles(repoPath, "*.*").Any())
        {
            var headRef = repo.Head.Reference.TargetIdentifier;
            DefaultBranch = repo.Branches
                .Where(b => b.Reference.TargetIdentifier == headRef)
                .Where(b => b.FriendlyName is "origin/main" or "origin/master")
                .FirstOrDefault()
                .FriendlyName.Split('/').Last();
            repository = repo;
            return;
        }

        Console.WriteLine($"Cloning repo {repoName}...");

        var remote = repo.Network.Remotes["origin"] ?? repo.Network.Remotes.Add("origin", cloneUrl);
        var refspecs = remote.FetchRefSpecs.Select(r => r.Specification);

        var fetchOpts = new FetchOptions
        {
            Prune = true,
            CredentialsProvider = CredentialsHandler,
            TagFetchMode = TagFetchMode.None
        };

        Commands.Fetch(repo, "origin", refspecs, fetchOpts, "Fetching remote");

        var defaultBranch = repo.Branches.First(b => b.FriendlyName is "origin/main" or "origin/master");

        Commands.Checkout(repo, defaultBranch);
        DefaultBranch = defaultBranch.FriendlyName.Split('/').Last();
    }

    Credentials CredentialsHandler(string url, string usernameFromUrl, SupportedCredentialTypes types)
    {
        return new UsernamePasswordCredentials
        {
            Username = AppConfig.GitHubToken,
            Password = string.Empty
        };
    }

    public IEnumerable<Document> GetDocuments()
    {
        var urlBase = $"https://github.com/Particular/{repoName}/tree/{DefaultBranch}/";
        var basePathLen = repoPath.Length + 1;

        foreach (var path in GetMarkdownPaths())
        {
            var relativePath = path.Substring(basePathLen);
            var url = urlBase + relativePath.Replace("\\", "/");
            yield return new Document(url, path);
        }
    }

    IEnumerable<string> GetMarkdownPaths() => GetMarkdownPaths(new DirectoryInfo(repoPath));

    IEnumerable<string> GetMarkdownPaths(DirectoryInfo current)
    {
        var files = current.GetFiles();

        if (files.Any(f => f.Name == ".skip-link-checks"))
        {
            yield break;
        }

        foreach (var file in current.GetFiles("*.md"))
        {
            yield return file.FullName;
        }

        foreach (var dir in current.GetDirectories())
        {
            if (dir.Name == ".git")
            {
                continue;
            }

            foreach (var item in GetMarkdownPaths(dir))
            {
                yield return item;
            }
        }
    }

    public string GetFilePath(string[] repoSegments) => FindFile(repoPath, repoSegments, 0);

    string FindFile(string localPath, string[] repoSegments, int segmentIndex)
    {
        var segment = repoSegments[segmentIndex];
        if (segment.IndexOf('%') >= 0)
        {
            segment = WebUtility.UrlDecode(segment);
        }
        var newPath = Path.Combine(localPath, segment);
        if (Path.Exists(newPath))
        {
            if (segmentIndex + 1 < repoSegments.Length)
            {
                return FindFile(newPath, repoSegments, segmentIndex + 1);
            }
            else
            {
                return newPath;
            }
        }

        return null;
    }
}
