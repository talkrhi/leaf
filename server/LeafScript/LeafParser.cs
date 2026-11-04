using System.Text.RegularExpressions;

namespace Leaf.LeafScript;

public static partial class LeafParser
{
    public static readonly HashSet<string> KnownTags =
        ["page", "slot", "include", "set", "if", "else", "nav", "mfm", "posts", "post", "comments"];

    [GeneratedRegex("""<leaf:(?<open>[a-z][a-z0-9-]*)(?<attrs>(?:\s+[a-z][a-z0-9-]*(?:="[^"]*")?)*)\s*(?<self>/?)>|</leaf:(?<close>[a-z][a-z0-9-]*)\s*>""")]
    private static partial Regex TagPattern();

    [GeneratedRegex("""(?<key>[a-z][a-z0-9-]*)(?:="(?<value>[^"]*)")?""")]
    private static partial Regex AttributePattern();

    public static List<LeafNode> Parse(string source)
    {
        var root = new List<LeafNode>();
        var open = new Stack<(TagNode Tag, int ContentStart)>();
        var position = 0;

        foreach (Match match in TagPattern().Matches(source))
        {
            var container = open.Count > 0 ? open.Peek().Tag.Children : root;
            if (match.Index > position) container.Add(new TextNode(source[position..match.Index]));
            position = match.Index + match.Length;
            var line = source.AsSpan(0, match.Index).Count('\n') + 1;

            if (match.Groups["close"].Success)
            {
                var name = match.Groups["close"].Value;
                if (open.Count == 0 || open.Peek().Tag.Name != name)
                    throw new LeafException($"line {line}: unexpected </leaf:{name}>");

                var (tag, contentStart) = open.Pop();
                tag.InnerSource = source[contentStart..match.Index];
                continue;
            }

            var node = new TagNode(match.Groups["open"].Value, ParseAttributes(match.Groups["attrs"].Value), line);
            if (!KnownTags.Contains(node.Name))
                throw new LeafException($"line {line}: there is no <leaf:{node.Name}> tag");

            container.Add(node);
            if (match.Groups["self"].Value != "/") open.Push((node, position));
        }

        if (open.TryPeek(out var unclosed))
        {
            var tag = unclosed.Tag;
            throw new LeafException($"line {tag.Line}: <leaf:{tag.Name}> is never closed (did you mean <leaf:{tag.Name} />?)");
        }

        if (position < source.Length) root.Add(new TextNode(source[position..]));
        return root;
    }

    private static Dictionary<string, string> ParseAttributes(string text)
    {
        var attributes = new Dictionary<string, string>();
        foreach (Match match in AttributePattern().Matches(text))
            attributes[match.Groups["key"].Value] = match.Groups["value"].Value;
        return attributes;
    }
}
