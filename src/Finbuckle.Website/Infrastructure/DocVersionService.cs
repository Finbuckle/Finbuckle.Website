using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using BaGetter.Protocol;
using BaGetter.Protocol.Models;

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

        var metaTasks = new List<Task<PackageMetadata>>(packages.Count);
        var metadata = new List<PackageMetadata>(packages.Count);
        foreach (var package in packages)
        {
            metaTasks.Add(nugetClient.GetPackageMetadataAsync("Finbuckle.MultiTenant", package));
        }

        await Task.WhenAll(metaTasks);
        metadata.AddRange(metaTasks.Select(t => t.Result));

        var getTasks = new List<(Task<HttpResponseMessage>, PackageMetadata)>(metaTasks.Count);

        foreach (var data in metadata.Where(m => m.Listed ?? false))
        {
            var version = data.ParseVersion().Version;

            var indexUrl =
                $"https://raw.githubusercontent.com/Finbuckle/Finbuckle.MultiTenant/v{version.ToString(3)}/docs/Index.md";
            getTasks.Add((HttpClient.GetAsync(indexUrl), data));
        }

        await Task.WhenAll();

        foreach (var (item1, taskMeta) in getTasks)
        {
            var response = item1.Result;
            if (!response.IsSuccessStatusCode) continue;

            var version = taskMeta.ParseVersion();

            var indexRaw = await response.Content.ReadAsStringAsync();
            var matches = Regex.Matches(indexRaw, @"^\[(.+)\]\((.+)\)$", RegexOptions.Multiline);
            var index = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                index[match.Groups[2].Value] = match.Groups[1].Value;
            }

            var docVersion = new DocVersion
            {
                Version = new Version(version.Version.Major, version.Version.Minor, version.Version.Build),
                ReleaseDate = taskMeta.Published,
                TargetFrameworks = taskMeta.DependencyGroups.Select(g => g.TargetFramework).OrderDescending()
                    .ToList(),
                Deprecated = taskMeta.Deprecation is not null,
                Index = index
            };
            versions.Add(docVersion);
        }

        Versions = versions.OrderByDescending(v => v.Version).ToList();
    }

    public IEnumerable<DocVersion> Versions { get; private set; } = Array.Empty<DocVersion>();
    public Version? LatestVersion => Versions.Select(v => v.Version).Max();

    public IEnumerable<DocVersion> LatestMajorVersions => Versions.GroupBy(v => v.Version.Major)
        .Select(g => g.MaxBy(dv => dv.Version))!;
}