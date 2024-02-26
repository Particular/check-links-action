namespace CheckLinks;

using System;
using System.Collections.Generic;
using System.Linq;

static class GitHubFS
{
    const StringComparison OIC = StringComparison.OrdinalIgnoreCase;

    static Dictionary<string, GitHubRepo> repos = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsValidGitHubUrl(Uri uri)
    {
        if (uri.Host != "github.com")
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/');
        string GetSegment(int i) => i < segments.Length ? segments[i] : null;

        var first = GetSegment(1);
        var second = GetSegment(2);

        if (first is "orgs" or "organizations" && second.Equals("Particular", OIC))
        {
            if (GetSegment(3) is "projects" or "teams" or "settings" or "policies" or "people")
            {
                return true;
            }
        }
        else if (first.Equals("Particular", OIC))
        {
            var repoName = GetSegment(2);
            var repoRoute = GetSegment(3);

            if (string.IsNullOrEmpty(repoRoute))
            {
                return true; // Repo root
            }
            if (repoRoute is "pull" or "pulls" or "issues")
            {
                var nextSegment = GetSegment(4);
                var query = uri.Query;
                _ = query;
                if (string.IsNullOrEmpty(nextSegment))
                {
                    return true;
                }
                if (int.TryParse(nextSegment, out _))
                {
                    return true;
                }
                if (repoRoute is "issues" && GetSegment(4) == "new")
                {
                    return true;
                }
            }
            else if (repoRoute is "blob" or "tree")
            {
                var branch = GetSegment(4);
                if (branch is "main" or "master")
                {
                    var repo = GetOrCloneRepo(repoName);
                    var filePath = repo.GetFilePath(segments.Skip(5).ToArray());
                    return filePath is not null;
                }
            }
            else if (repoRoute is "projects" or "branches" or "actions" or "settings" or "labels" or "commit" or "files" or "assets")
            {
                return true;
            }
        }

        return false;
    }

    static GitHubRepo GetOrCloneRepo(string repoName)
    {
        if (!repos.TryGetValue(repoName, out var repo))
        {
            repo = new GitHubRepo(repoName);
            repos.Add(repoName, repo);
        }

        repo.Clone();

        return repo;
    }

    public static IEnumerable<Document> GetDocuments(string repoName)
    {
        var repo = GetOrCloneRepo(repoName);
        return repo.GetDocuments();
    }

    public static string GetDefaultBranchName(string repoName)
    {
        var repo = GetOrCloneRepo(repoName);
        return repo.DefaultBranch;
    }
}
