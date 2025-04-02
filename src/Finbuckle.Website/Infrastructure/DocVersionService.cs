using System.Text.RegularExpressions;

namespace Finbuckle.Website.Infrastructure;

public class DocVersionService(IWebHostEnvironment webHostEnvironment, ILogger<DocVersionService> logger)
{
    public class DocVersion
    {
        public Version Version { get; init; } = new();
        public Dictionary<string, string> Index { get; init; } = [];
    }

    public async Task LoadAsync()
    {
        var fileProvider = webHostEnvironment.ContentRootFileProvider;
        
        var docFolders = fileProvider.
            GetDirectoryContents("content/docs").
            Select(f => f.Name);
        
        var versions = new List<DocVersion>();

        foreach (var docFolder in docFolders)
        {
            logger.LogInformation("Processing folder: {docFolder}", docFolder);
            
            var indexStream = fileProvider.GetFileInfo($"content/docs/{docFolder}/Index.md").CreateReadStream();
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