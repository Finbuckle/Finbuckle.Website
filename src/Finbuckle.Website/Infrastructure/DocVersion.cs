namespace Finbuckle.Website.Infrastructure;

public class DocVersion
{
    public Version Version { get; init; } = new();
    public Dictionary<string, string> Index { get; init; } = [];
}