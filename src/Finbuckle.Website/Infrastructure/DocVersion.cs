namespace Finbuckle.Website.Infrastructure;

public class DocVersion
{
    public Version Version { get; init; } = new();
    public DateTimeOffset ReleaseDate { get; init; }
    public IList<string> TargetFrameworks { get; init; } = default!;
    public bool Deprecated { get; init; }
    public Dictionary<string, string> Index { get; init; } = [];
}