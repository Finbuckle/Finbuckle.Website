using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Finbuckle.Website.Infrastructure;

public class BlogService
{
    public class BlogInfo
    {
        public string Date { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string[] Tags { get; init; } = [];
    }
    
    public BlogService(IWebHostEnvironment webHostEnvironment, ILogger<BlogService> logger)
    {
        WebHostEnvironment = webHostEnvironment;
        Logger = logger;
    }

    private IWebHostEnvironment WebHostEnvironment { get; }
    private ILogger<BlogService> Logger { get; }

    public async Task LoadAsync()
    {
        var fileProvider = WebHostEnvironment.ContentRootFileProvider;
        
        var files = fileProvider.
            GetDirectoryContents("content/blog").
            Select(f => f.Name);

        var yamlDeserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

        foreach (var file in files)
        {
            Logger.LogInformation($"Processing file: {file}");
            
            var fileStream = fileProvider.GetFileInfo($"content/blog/{file}").CreateReadStream();
            using var reader = new StreamReader(fileStream);
            var content = await reader.ReadToEndAsync();
            
            // extract the YAML front matter string
            var match = Regex.Match(content, @"^---\s*([\s\S]+?)\s*---", RegexOptions.Multiline);
            if (match.Success)
            {
                var yaml = match.Groups[1].Value;
                var blogInfo = yamlDeserializer.Deserialize<BlogInfo>(yaml);
                
                // process the blog info into an index
            }
            else
            {
                Logger.LogWarning($"No YAML front matter found in file: {file}");
            }
        }
    }
}