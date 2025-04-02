using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Finbuckle.Website.Infrastructure;

public record BlogData
{
    public string FilePath { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string[] Tags { get; init; } = [];
}

public class BlogService
{
    public BlogService(IWebHostEnvironment webHostEnvironment, ILogger<BlogService> logger)
    {
        WebHostEnvironment = webHostEnvironment;
        Logger = logger;
    }

    private IWebHostEnvironment WebHostEnvironment { get; }
    private ILogger<BlogService> Logger { get; }

    public List<BlogData> BlogData { get; private set; }= [];

    public async Task LoadAsync()
    {
        var newBlogData = new List<BlogData>();

        var fileProvider = WebHostEnvironment.ContentRootFileProvider;

        var files = fileProvider.GetDirectoryContents("content/blog").Select(f => f.Name);

        var yamlDeserializer =
            new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

        foreach (var file in files)
        {
            var filePath = $"content/blog/{file}";
            Logger.LogInformation("Processing file: {filePath}", filePath);
            
            var content = await File.ReadAllTextAsync(filePath);

            var match = Regex.Match(content, @"^---\s*([\s\S]+?)\s*---");
            if (match.Success)
            {
                try
                {
                    var yaml = match.Groups[1].Value;
                    var blogData = yamlDeserializer.Deserialize<BlogData>(yaml);
                    blogData = blogData with { FilePath = filePath };

                    newBlogData.Add(blogData);
                }
                catch (Exception e)
                {
                    Logger.LogError("Error parsing YAML in file: {file}", filePath);
                }
            }
            else
            {
                Logger.LogWarning("No YAML front matter found in file: {file}", filePath);
            }
        }

        BlogData = newBlogData;
    }

    public BlogData? GetBlogDataBySlug(string slug) =>
        BlogData.FirstOrDefault(b => string.Equals(slug, b.Slug, StringComparison.OrdinalIgnoreCase));

    public ILookup<string, BlogData> GetBlogDataTagLookup =>
        BlogData.SelectMany(b => b.Tags.Select(t => t.ToLowerInvariant()), (data, tag) => new { data, tag })
            .ToLookup(e => e.tag, e => e.data);

    public async Task<string?> GetBlogMarkdownBySlugAsync(string slug)
    {
        var blogData = GetBlogDataBySlug(slug);
        if (blogData == null) return null;
        var filePath = blogData.FilePath;
        var content = await File.ReadAllTextAsync(filePath);
        var match = Regex.Match(content, @"^---\s*([\s\S]+?)\s*---");
        content = content.Substring(match.Length).Trim();

        return content;
    }
}