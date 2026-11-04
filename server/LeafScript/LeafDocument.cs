namespace Leaf.LeafScript;

public sealed class LeafDocument
{
    public LeafDocument(string name, List<LeafNode> nodes)
    {
        Name = name;
        Nodes = nodes;

        var page = nodes.OfType<TagNode>().FirstOrDefault(tag => tag.Name == "page");
        var attributes = page?.Attributes ?? [];
        Title = attributes.GetValueOrDefault("title") ?? name;
        Nav = attributes.GetValueOrDefault("nav");
        Order = int.TryParse(attributes.GetValueOrDefault("order"), out var order) ? order : 100;
        Layout = attributes.GetValueOrDefault("layout") ?? "_layout";
    }

    public string Name { get; }
    public List<LeafNode> Nodes { get; }
    public string Title { get; }
    public string? Nav { get; }
    public int Order { get; }
    public string Layout { get; }
}
