using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Leaf.Auth;
using Leaf.Data;

namespace Leaf.LeafScript;

public sealed partial class LeafEngine(LeafFiles files, SettingsStore settings, PostStore posts, CommentStore comments)
{
    public LeafFiles Files => files;

    public IResult Render(HttpContext http, string page, Post? post = null, int status = StatusCodes.Status200OK)
    {
        var ctx = CreateContext(http, post);
        string html;
        try
        {
            html = RenderWithLayout(files.Load(page), ctx);
        }
        catch (LeafException ex)
        {
            html = $"""
                <!doctype html><title>Leaf error</title>
                <pre style="padding:2rem;color:#c92a2a;white-space:pre-wrap">Leaf error in {Encode(ex.File ?? page)}.leaf, {Encode(ex.Message)}</pre>
                """;
            status = StatusCodes.Status500InternalServerError;
        }
        return Results.Content(html, "text/html", Encoding.UTF8, status);
    }

    public IResult NotFound(HttpContext http) =>
        files.Exists("404") ? Render(http, "404", status: StatusCodes.Status404NotFound) : Results.NotFound();

    private LeafContext CreateContext(HttpContext http, Post? post)
    {
        var site = settings.Get();
        var ctx = new LeafContext(http, site) { Post = post };

        ctx.Vars["site.title"] = site.Title;
        ctx.Vars["site.description"] = site.Description;
        ctx.Vars["user.name"] = http.User.Identity?.Name ?? "";
        ctx.Vars["user.role"] = http.User.Role();
        ctx.Vars["year"] = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        foreach (var (key, value) in http.Request.Query) ctx.Vars["query." + key] = value.ToString();

        if (post is not null)
        {
            ctx.Vars["post.id"] = post.Id.ToString(CultureInfo.InvariantCulture);
            ctx.Vars["post.title"] = post.Title;
            ctx.Vars["post.slug"] = post.Slug;
        }
        return ctx;
    }

    private string RenderWithLayout(LeafDocument page, LeafContext ctx)
    {
        ctx.Vars["page.name"] = page.Name;
        ctx.Vars["page.title"] = ctx.Post?.Title ?? page.Title;

        var body = new StringBuilder();
        RenderDocument(page, ctx, body);
        if (page.Layout == "none") return body.ToString();

        ctx.Slot = body.ToString();
        var html = new StringBuilder();
        RenderDocument(files.Load(page.Layout), ctx, html);
        return html.ToString();
    }

    private void RenderDocument(LeafDocument document, LeafContext ctx, StringBuilder output)
    {
        try
        {
            Render(document.Nodes, ctx, output);
        }
        catch (LeafException ex)
        {
            ex.File ??= document.Name;
            throw;
        }
    }

    private void Render(List<LeafNode> nodes, LeafContext ctx, StringBuilder output)
    {
        foreach (var node in nodes)
        {
            if (node is TextNode text) output.Append(Interpolate(text.Text, ctx, encode: true));
            else if (node is TagNode tag) RenderTag(tag, ctx, output);
        }
    }

    [GeneratedRegex(@"\{\{\s*([\w.-]+)\s*\}\}")]
    private static partial Regex VariablePattern();

    private static string Interpolate(string text, LeafContext ctx, bool encode) =>
        !text.Contains("{{")
            ? text
            : VariablePattern().Replace(text, match =>
            {
                var value = ctx.Var(match.Groups[1].Value);
                return encode ? Encode(value) : value;
            });

    private static string Attr(TagNode tag, string name, LeafContext ctx, string fallback = "") =>
        tag.Attributes.TryGetValue(name, out var raw) ? Interpolate(raw, ctx, encode: false) : fallback;

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string FormatDate(string iso) =>
        DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
            ? date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)
            : iso;
}
