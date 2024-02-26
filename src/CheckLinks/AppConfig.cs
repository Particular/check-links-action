namespace CheckLinks;

using System;
using System.Linq;

public static class AppConfig
{
    public static string GitHubToken { get; }
    public static string RepoName { get; }
    public static string CheckoutDirectory { get; }
    public static bool CreateIssue { get; }
    public static string CreateIssueInOtherRepo { get; }
    public static bool FailOnBrokenLinks { get; }

    static AppConfig()
    {
        GitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        RepoName = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")?.Split('/').FirstOrDefault();

#if DEBUG
        RepoName ??= "StaffSuccess";
#else
        if (string.IsNullOrEmpty(RepoName))
        {
            throw new Exception("GITHUB_REPOSITORY not specified");
        }
#endif

        var runnerWorkspace = Environment.GetEnvironmentVariable("RUNNER_WORKSPACE");
        if (runnerWorkspace is not null)
        {
            // On Actions
            CheckoutDirectory = Path.Combine(runnerWorkspace, "co");
        }
        else
        {
            // Can't be in bin directory because that's already inside a git repo
            CheckoutDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "check-links");
        }
        Console.WriteLine($"Using {CheckoutDirectory} for repository checkouts.");
        Directory.CreateDirectory(CheckoutDirectory);

        CreateIssue = Environment.GetEnvironmentVariable("CREATE_ISSUE") == "true";
        CreateIssueInOtherRepo = Environment.GetEnvironmentVariable("CREATE_ISSUE_IN_OTHER_REPO");
        FailOnBrokenLinks = Environment.GetEnvironmentVariable("FAIL_ON_BROKEN_LINKS") == "true";

        if (string.IsNullOrWhiteSpace(CreateIssueInOtherRepo))
        {
            CreateIssueInOtherRepo = null;
        }
    }
}
