using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using BaGetter.Protocol;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.FileProviders;

namespace Finbuckle.Website.Infrastructure;

public class DocVersionService
{
    public DocVersionService(IWebHostEnvironment webHostEnvironment)
    {
        WebHostEnvironment = webHostEnvironment;
    }

    // private static HttpClient HttpClient
    // {
    //     get
    //     {
    //         var client = new HttpClient();
    //         client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Finbuckle.Website", null));
    //
    //         return client;
    //     }
    // }
    
    private IWebHostEnvironment WebHostEnvironment { get; }

    public async Task LoadAsync()
    {
        var fileProvider = WebHostEnvironment.ContentRootFileProvider;
        
        var docFolders = fileProvider.
            GetDirectoryContents("docs").
            Select(f => f.Name);
        
        var versions = new List<DocVersion>();

        foreach (var docFolder in docFolders)
        {
            var indexStream = fileProvider.GetFileInfo($"docs/{docFolder}/Index.md").CreateReadStream();
            using var reader = new StreamReader(indexStream);
            var indexRaw = await reader.ReadToEndAsync();
            
            var matches = Regex.Matches(indexRaw, @"^\[(.+)\]\((.+)\)$", RegexOptions.Multiline);
            var index = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                index[match.Groups[2].Value] = match.Groups[1].Value;
            }

            
            var docVersion = new DocVersion
            {
                Version = Version.Parse(docFolder[1..]),
                Index = index
            };
            versions.Add(docVersion);
        }

        Versions = versions.OrderByDescending(v => v.Version).ToList();
    }

    public IEnumerable<DocVersion> Versions { get; private set; } = [];
    public Version LatestVersion => Versions.Select(v => v.Version).Max() ?? new Version();

    public IEnumerable<DocVersion> LatestMajorVersions => Versions.GroupBy(v => v.Version.Major)
        .Select(g => g.MaxBy(dv => dv.Version))!;
}