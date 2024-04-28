using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using BaGetter.Protocol;

namespace Finbuckle.Website.Infrastructure;

public class DocVersionService
{
    private static HttpClient HttpClient
    {
        get
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Finbuckle.Website", null));

            return client;
        }
    }

    public async Task LoadAsync()
    {
        var nugetClient = new NuGetClient("https://api.nuget.org/v3/index.json");
        var packages = await nugetClient.ListPackageVersionsAsync("Finbuckle.MultiTenant", true);

        var versions = new List<DocVersion>();

        foreach (var package in packages)
        {
            var meta = await nugetClient.GetPackageMetadataAsync("Finbuckle.MultiTenant", package);
            if(!meta.Listed!.Value) continue;
            
            var version = new Version(package.Version.Major, package.Version.Minor, package.Version.Build);

            var indexUrl =
                $"https://raw.githubusercontent.com/Finbuckle/Finbuckle.MultiTenant/v{version}/docs/Index.md";
            var response = await HttpClient.GetAsync(indexUrl);
            if (!response.IsSuccessStatusCode) continue;

            var indexRaw = await response.Content.ReadAsStringAsync();
            var matches = Regex.Matches(indexRaw, @"^\[(.+)\]\((.+)\)$", RegexOptions.Multiline);
            var index = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                index[match.Groups[2].Value] = match.Groups[1].Value;
            }
            
            var docVersion = new DocVersion
            {
                Version = version,
                ReleaseDate = meta.Published,
                TargetFrameworks = meta.DependencyGroups.Select(g => g.TargetFramework).OrderDescending()
                    .ToList(),
                Deprecated = meta.Deprecation is not null,
                Index = index
            };
            versions.Add(docVersion);
        }

        Versions = versions.OrderByDescending(v => v.Version);
    }

    public IEnumerable<DocVersion> Versions { get; private set; } = Array.Empty<DocVersion>();
    public Version LatestVersion => Versions.Select(v => v.Version).Max()!;
    public IEnumerable<DocVersion> LatestMajorVersions => Versions.GroupBy(v => v.Version.Major)
        .Select(g => g.MaxBy(dv => dv.Version))!;
    public IEnumerable<DocVersion> LatestMinorVersions => Versions.GroupBy(v => (v.Version.Major, v.Version.Minor))
        .Select(g => g.MaxBy(dv => dv.Version))!;
}