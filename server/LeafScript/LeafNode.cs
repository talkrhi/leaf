namespace Leaf.LeafScript;

public abstract class LeafNode
{
}

public sealed class TextNode(string text) : LeafNode
{
    public string Text { get; } = text;
}

public sealed class TagNode(string name, Dictionary<string, string> attributes, int line) : LeafNode
{
    public string Name { get; } = name;
    public Dictionary<string, string> Attributes { get; } = attributes;
    public int Line { get; } = line;
    public List<LeafNode> Children { get; } = [];
    public string InnerSource { get; set; } = "";
}

public sealed class LeafException(string message) : Exception(message)
{
    public string? File { get; set; }
}
