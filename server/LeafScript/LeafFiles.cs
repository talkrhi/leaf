using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Leaf.LeafScript;

public sealed partial class LeafFiles
{
    private readonly string _directory;
    private readonly ConcurrentDictionary<string, (DateTime Stamp, LeafDocument Document)> _cache = new();

    public LeafFiles(string directory, string themeDirectory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);

        if (!Directory.EnumerateFiles(directory, "*.leaf").Any())
        {
            foreach (var file in Directory.EnumerateFiles(themeDirectory, "*.leaf"))
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
        }
    }

    [GeneratedRegex("^_?[a-z0-9][a-z0-9-]{0,63}$")]
    private static partial Regex NamePattern();

    public static bool IsValidName(string name) => NamePattern().IsMatch(name);

    public bool Exists(string name) => IsValidName(name) && File.Exists(PathOf(name));

    public bool IsRoutable(string name) => !name.StartsWith('_') && Exists(name);

    public string Read(string name) => File.ReadAllText(PathOf(name));

    public IEnumerable<string> Names() =>
        Directory.EnumerateFiles(_directory, "*.leaf")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Where(IsValidName)
            .Order();

    public void Write(string name, string source)
    {
        LeafParser.Parse(source);
        File.WriteAllText(PathOf(name), source);
        _cache.TryRemove(name, out _);
    }

    public void Delete(string name)
    {
        File.Delete(PathOf(name));
        _cache.TryRemove(name, out _);
    }

    public LeafDocument Load(string name)
    {
        if (!Exists(name)) throw new LeafException($"the page \"{name}\" doesn't exist");

        var path = PathOf(name);
        var stamp = File.GetLastWriteTimeUtc(path);
        if (_cache.TryGetValue(name, out var cached) && cached.Stamp == stamp) return cached.Document;

        try
        {
            var document = new LeafDocument(name, LeafParser.Parse(File.ReadAllText(path)));
            _cache[name] = (stamp, document);
            return document;
        }
        catch (LeafException ex)
        {
            ex.File ??= name;
            throw;
        }
    }

    public LeafDocument? TryLoad(string name)
    {
        try { return Load(name); }
        catch (LeafException) { return null; }
    }

    private string PathOf(string name) =>
        IsValidName(name)
            ? Path.Combine(_directory, name + ".leaf")
            : throw new LeafException($"\"{name}\" is not a valid page name");
}
