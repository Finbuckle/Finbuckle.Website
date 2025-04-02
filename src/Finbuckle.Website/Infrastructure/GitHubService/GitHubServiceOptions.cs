namespace Finbuckle.Website.Infrastructure.GitHubService;

public class GitHubServiceOptions
{
    public required string Token { get; init; }
    public required string Organization { get; init; }
}