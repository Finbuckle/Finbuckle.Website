using System.Net.Http.Headers;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Internal;

namespace Finbuckle.Website.Infrastructure.GitHubService;

public class GitHubService(IOptionsMonitor<GitHubServiceOptions> options)
{
    public IEnumerable<SponsorOrContributor> Sponsors { get; private set; } = [];
    public IEnumerable<SponsorOrContributor> Contributors { get; private set; } = [];
    public IEnumerable<SponsorOrContributor> SponsorsAndContributors =>
        Sponsors.Concat(Contributors).DistinctBy(e => e.Login).OrderBy(e => e.Login);

    public async Task LoadAsync()
    {
        Contributors = await GetContributorsAsync();
        Sponsors = await GetSponsorsAsync();
    }

    private async Task<List<SponsorOrContributor>> GetContributorsAsync()
    {
        var creds = new InMemoryCredentialStore(new Credentials(options.CurrentValue.Token));
        var client = new GitHubClient(new Octokit.ProductHeaderValue("Finbuckle.Website"), creds);
        
        var list = new List<RepositoryContributor>();
        foreach (var repo in await client.Repository.GetAllForOrg(options.CurrentValue.Organization))
        {
            var contributors = await client.Repository.GetAllContributors(repo.Owner.Login, repo.Name);
            list.AddRange(contributors);
        }

        return list.DistinctBy(c => c.Login).OrderBy(c => c.Login)
            .Select(c => new SponsorOrContributor { Login = c.Login, AvatarUrl = c.AvatarUrl })
            .ToList();
    }

    private async Task<List<SponsorOrContributor>> GetSponsorsAsync()
    {
        var client = new GraphQLHttpClient("https://api.github.com/graphql", new SystemTextJsonSerializer());
        var cursor = string.Empty;
        var hasMorePages = true;
        var sponsors = new List<SponsorOrContributor>();

        while (hasMorePages)
        {
            var query = $$"""
                          {
                            organization(login: "{{options.CurrentValue.Organization}}") {
                              sponsorshipsAsMaintainer(first: 50, after:"{{cursor}}", activeOnly: false, includePrivate: true) {
                                pageInfo {
                                  endCursor
                                  hasNextPage
                                }
                                nodes {
                                  privacyLevel
                                  sponsorEntity {
                                    ... on Actor {
                                      login
                                      avatarUrl
                                    }
                                  }
                                }
                              }
                            }
                          }
                          """;

            var request = new GraphQLRequest(query);

            client.HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.CurrentValue.Token);
            var response = await client.SendQueryAsync<Data>(request);

            sponsors.AddRange(response.Data.organization.sponsorshipsAsMaintainer.nodes.Select(n =>
                new SponsorOrContributor
                {
                    Login = n.privacyLevel == "PUBLIC" ? n.sponsorEntity.login : "Anonymous",
                    AvatarUrl = n.privacyLevel == "PUBLIC" ? n.sponsorEntity.avatarUrl : string.Empty,
                    PrivacyLevel = n.privacyLevel
                }));

            hasMorePages = response.Data.organization.sponsorshipsAsMaintainer.pageInfo.hasNextPage;
            cursor = response.Data.organization.sponsorshipsAsMaintainer.pageInfo.endCursor;
        }

        return sponsors.OrderByDescending(s => s.PrivacyLevel).ThenBy(s => s.Login).ToList();
    }
}