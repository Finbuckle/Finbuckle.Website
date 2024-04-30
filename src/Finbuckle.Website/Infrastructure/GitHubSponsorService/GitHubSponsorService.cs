using System.Net.Http.Headers;
using GraphQL;
using GraphQL.Client.Http;
using Microsoft.Extensions.Options;

namespace Finbuckle.Website.Infrastructure.GitHubSponsorService;

public class GitHubSponsorService(GraphQLHttpClient client, IOptionsSnapshot<GitHubSponsorServiceOptions> options)
{
    public async Task<List<Sponsor>> GetSponsorsAsync()
    {
        var cursor = string.Empty;
        var hasMorePages = true;
        var sponsors = new List<Sponsor>();

        while (hasMorePages)
        {
            var query = $$"""
                          {
                            organization(login: "{{options.Value.Organization}}") {
                              sponsorshipsAsMaintainer(first: 1, after:"{{cursor}}") {
                                pageInfo {
                                  endCursor
                                  hasNextPage
                                }
                                nodes {
                                  privacyLevel
                                  sponsorEntity {
                                    type: __typename
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
                new AuthenticationHeaderValue("Bearer", options.Value.Token);
            var response = await client.SendQueryAsync<Data>(request);

            sponsors.AddRange(response.Data.organization.sponsorshipsAsMaintainer.nodes.Select(n => new Sponsor
            {
                Login = n.sponsorEntity.login,
                AvatarUrl = n.sponsorEntity.avatarUrl,
                PrivacyLevel = n.privacyLevel
            }));

            hasMorePages = response.Data.organization.sponsorshipsAsMaintainer.pageInfo.hasNextPage;
            cursor = response.Data.organization.sponsorshipsAsMaintainer.pageInfo.endCursor;
        }

        return sponsors;
    }
}