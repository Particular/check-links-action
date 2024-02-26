namespace CheckLinks;

using System;
using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;

public static class GitHubApi
{
    public static async Task CreateIssueIfNecessary(string reportMarkdown)
    {
        if (AppConfig.CreateIssue)
        {
            return;
        }

        var github = GetClient();
        _ = github;

        var newIssue = new NewIssue($"Check for broken links in {AppConfig.RepoName}")
        {
            Body = reportMarkdown
        };

        var createInRepo = AppConfig.CreateIssueInOtherRepo ?? AppConfig.RepoName;

        //await github.Issue.Create("Particular", createInRepo, newIssue);
        await Task.Yield();

        Console.WriteLine($"Creating issue '{newIssue.Title}' in repo {createInRepo}...");
    }

    static GitHubClient GetClient()
    {
        var connection = new Connection(
            new ProductHeaderValue("ParticularAutomation"),
            new Uri("https://api.github.com/"),
            new InMemoryCredentialStore(new Credentials(AppConfig.GitHubToken)));
        return new GitHubClient(connection);
    }
}
