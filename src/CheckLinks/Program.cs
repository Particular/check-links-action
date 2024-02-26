namespace CheckLinks;

using System.Text;

class Program
{
    public static async Task<int> Main()
    {
        var documents = GitHubFS.GetDocuments(AppConfig.RepoName).ToArray();

        foreach (var doc in documents)
        {
            doc.ExtractLinks();
        }

        foreach (var link in documents.SelectMany(doc => doc.Links))
        {
            link.Validate();
        }

        var invalidLinks = documents.SelectMany(doc => doc.Links.Where(link => !link.IsValid)).ToArray();

        foreach (var invalid in invalidLinks)
        {
            invalid.Suggest();
        }

        if (invalidLinks.Any())
        {
            var b = new StringBuilder();

            foreach (var doc in documents)
            {
                if (doc.Links.Any(l => !l.IsValid))
                {
                    b.AppendLine($"- [{doc.GitHubPath}]({doc.Url})");

                    foreach (var link in doc.Links.Where(l => !l.IsValid))
                    {
                        b.Append($"  - [ ] [{link.Text}]({link.Uri.AbsoluteUri})");
                        if (link.Reason is not null)
                        {
                            b.Append($" ({link.Reason})");
                        }
                        if (link.SuggestedReplacement is not null)
                        {
                            b.Append($" :arrow_right: [Suggested Replacement]({link.SuggestedReplacement})");
                        }
                        b.AppendLine();
                    }
                }
            }

            var report = b.ToString();

            Console.WriteLine(report);

            await GitHubApi.CreateIssueIfNecessary(report);

            if (AppConfig.FailOnBrokenLinks)
            {
                return -1;
            }
        }
        else
        {
            Console.WriteLine("No broken GitHub links detected.");
        }

        return 0;
    }


}
