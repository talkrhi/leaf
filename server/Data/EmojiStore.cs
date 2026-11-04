using System.Text.RegularExpressions;

namespace Leaf.Data;

public sealed partial class EmojiStore
{
    public const long MaxSize = 1024 * 1024;
    public static readonly string[] Extensions = [".png", ".gif", ".webp", ".jpg", ".jpeg"];

    private readonly string _directory;

    public EmojiStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    [GeneratedRegex("^[a-z0-9_-]{1,32}$")]
    private static partial Regex NamePattern();

    public static bool IsValidName(string name) => NamePattern().IsMatch(name);

    public List<Emoji> List() =>
        Images()
            .Select(file => new Emoji
            {
                Name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant(),
                Url = "/emojis/" + Path.GetFileName(file),
            })
            .Where(emoji => IsValidName(emoji.Name))
            .DistinctBy(emoji => emoji.Name)
            .OrderBy(emoji => emoji.Name)
            .ToList();

    public bool Exists(string name) => ImagesOf(name).Any();

    public async Task<Emoji> AddAsync(string name, string extension, Stream image)
    {
        var fileName = name + extension;
        await using var file = File.Create(Path.Combine(_directory, fileName));
        await image.CopyToAsync(file);
        return new Emoji { Name = name, Url = "/emojis/" + fileName };
    }

    public void Delete(string name)
    {
        foreach (var file in ImagesOf(name)) File.Delete(file);
    }

    private IEnumerable<string> Images() =>
        Directory.EnumerateFiles(_directory)
            .Where(file => Extensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

    private IEnumerable<string> ImagesOf(string name) =>
        IsValidName(name)
            ? Images().Where(file => string.Equals(Path.GetFileNameWithoutExtension(file), name, StringComparison.OrdinalIgnoreCase))
            : [];
}
