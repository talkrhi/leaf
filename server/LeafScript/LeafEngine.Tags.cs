using System.Text;
using Leaf.Auth;
using Leaf.Data;

namespace Leaf.LeafScript;

public sealed partial class LeafEngine
{
    private void RenderTag(TagNode tag, LeafContext ctx, StringBuilder output)
    {
        switch (tag.Name)
        {
            case "page" or "else": break;
            case "slot": output.Append(ctx.Slot); break;
            case "set": ctx.Vars[Attr(tag, "name", ctx)] = Attr(tag, "value", ctx); break;
            case "include": Include(tag, ctx, output); break;
            case "if": If(tag, ctx, output); break;
            case "nav": Nav(ctx, output); break;
            case "mfm": Mfm(Dedent(tag.InnerSource), output); break;
            case "posts": PostList(tag, ctx, output); break;
            case "post": PostBody(tag, ctx, output); break;
            case "comments": CommentList(tag, ctx, output); break;
            default: throw new LeafException($"line {tag.Line}: there is no <leaf:{tag.Name}> tag");
        }
    }

    private void Include(TagNode tag, LeafContext ctx, StringBuilder output)
    {
        if (ctx.Depth >= 8) throw new LeafException($"line {tag.Line}: includes are nested too deeply");
        ctx.Depth++;
        RenderDocument(files.Load(Attr(tag, "src", ctx)), ctx, output);
        ctx.Depth--;
    }

    private void If(TagNode tag, LeafContext ctx, StringBuilder output)
    {
        var split = tag.Children.FindIndex(node => node is TagNode { Name: "else" });
        var whenTrue = split < 0 ? tag.Children : tag.Children.GetRange(0, split);
        var whenFalse = split < 0 ? [] : tag.Children.GetRange(split + 1, tag.Children.Count - split - 1);
        Render(Test(tag, ctx) ? whenTrue : whenFalse, ctx, output);
    }

    private static bool Test(TagNode tag, LeafContext ctx)
    {
        var expression = Attr(tag, "test", ctx).Trim();
        var negate = expression.StartsWith('!');
        var name = expression.TrimStart('!');

        var result = name switch
        {
            "signed-in" => ctx.SignedIn,
            "anon" => !ctx.SignedIn,
            "member" => ctx.SignedIn && ctx.User.Role() != "guest",
            "admin" or "user" or "guest" => ctx.User.Role() == name,
            "registration" => ctx.Settings.AllowRegistration,
            "guests" => ctx.Settings.AllowGuests,
            "comments" => ctx.Settings.AllowComments,
            "post" => ctx.Post is not null,
            _ when name.Contains('.') => ctx.Var(name).Length > 0,
            _ => throw new LeafException($"line {tag.Line}: unknown test \"{name}\""),
        };
        return result != negate;
    }

    private void Nav(LeafContext ctx, StringBuilder output)
    {
        var pages = files.Names()
            .Where(name => !name.StartsWith('_'))
            .Select(files.TryLoad)
            .OfType<LeafDocument>()
            .Where(page => page.Nav is not null)
            .OrderBy(page => page.Order);

        output.Append("<nav class=\"leaf-nav\">");
        foreach (var page in pages)
        {
            var href = page.Name == "index" ? "/" : "/" + page.Name;
            var current = page.Name == ctx.Var("page.name") ? " aria-current=\"page\"" : "";
            output.Append($"<a href=\"{href}\"{current}>{Encode(page.Nav!)}</a>");
        }
        output.Append("</nav>");
    }

    private void PostList(TagNode tag, LeafContext ctx, StringBuilder output)
    {
        var limit = int.TryParse(Attr(tag, "limit", ctx), out var l) ? Math.Clamp(l, 1, 50) : 10;
        var filter = Attr(tag, "tag", ctx, fallback: ctx.Var("query.tag")).Trim().ToLowerInvariant();
        var page = int.TryParse(ctx.Var("query.page"), out var p) && p > 1 ? p : 1;
        var list = posts.List(includeDrafts: false, filter, skip: (page - 1) * limit, take: limit + 1);

        if (filter.Length > 0)
        {
            output.Append($"<p class=\"muted\">Posts tagged <strong>#{Encode(filter)}</strong> | ")
                  .Append($"<a href=\"{Encode(ctx.Http.Request.Path)}\">show all</a></p>");
        }
        if (list.Count == 0) output.Append("<p class=\"muted\">Nothing here yet.</p>");

        foreach (var post in list.Take(limit))
        {
            var url = "/post/" + Encode(post.Slug);
            var excerpt = Excerpt(post.Body);
            output.Append($"<article class=\"post-card\"><h2><a href=\"{url}\">{Encode(post.Title)}</a></h2>");
            PostMeta(post, ctx, output);
            Mfm(excerpt, output);
            if (excerpt.Length < post.Body.Length) output.Append($"<a class=\"read-more\" href=\"{url}\">Read more →</a>");
            output.Append("</article>");
        }

        if (page > 1 || list.Count > limit)
        {
            var tagQuery = filter.Length > 0 ? $"tag={Uri.EscapeDataString(filter)}&amp;" : "";
            output.Append("<nav class=\"pager\">");
            if (page > 1) output.Append($"<a href=\"?{tagQuery}page={page - 1}\">← Newer</a>");
            if (list.Count > limit) output.Append($"<a class=\"older\" href=\"?{tagQuery}page={page + 1}\">Older →</a>");
            output.Append("</nav>");
        }
    }

    private static void PostBody(TagNode tag, LeafContext ctx, StringBuilder output)
    {
        var post = ctx.Post ?? throw new LeafException($"line {tag.Line}: <leaf:post /> only works on post.leaf");
        output.Append($"<article class=\"post\"><header><h1>{Encode(post.Title)}</h1>");
        PostMeta(post, ctx, output);
        output.Append("</header>");
        Mfm(post.Body, output);
        output.Append("</article>");
    }

    private void CommentList(TagNode tag, LeafContext ctx, StringBuilder output)
    {
        var post = ctx.Post ?? throw new LeafException($"line {tag.Line}: <leaf:comments /> only works on post.leaf");
        var list = comments.ForPost(post.Id);
        if (list.Count == 0)
        {
            output.Append("<p class=\"muted\">No comments yet.</p>");
            return;
        }

        output.Append("<ol class=\"comment-list\">");
        foreach (var comment in list)
        {
            output.Append($"<li class=\"comment\"><div class=\"comment-meta\"><strong>{Encode(comment.Username)}</strong>");
            if (comment.Role != "user") output.Append($" <span class=\"badge\">{Encode(comment.Role)}</span>");
            output.Append($" | {FormatDate(comment.CreatedAt)}");
            if (ctx.IsAdmin || ctx.User.UserId() == comment.UserId)
                output.Append($" | <button class=\"link\" data-leaf-action=\"delete-comment\" data-id=\"{comment.Id}\">delete</button>");
            output.Append("</div>");
            Mfm(comment.Body, output, limited: true);
            output.Append("</li>");
        }
        output.Append("</ol>");
    }

    private static void PostMeta(Post post, LeafContext ctx, StringBuilder output)
    {
        output.Append($"<p class=\"post-meta\"><time datetime=\"{Encode(post.CreatedAt)}\">{FormatDate(post.CreatedAt)}</time>");
        output.Append($" | {Encode(post.AuthorName)}");
        foreach (var tag in post.TagList)
            output.Append($" <a class=\"tag\" href=\"/?tag={Uri.EscapeDataString(tag)}\">#{Encode(tag)}</a>");
        if (!post.Published) output.Append(" <span class=\"badge\">draft</span>");
        if (ctx.IsAdmin) output.Append($" | <a href=\"/admin#posts/{post.Id}\">edit</a>");
        output.Append("</p>");
    }

    private static void Mfm(string source, StringBuilder output, bool limited = false)
    {
        output.Append(limited ? "<div class=\"mfm\" data-mfm=\"limited\">" : "<div class=\"mfm\" data-mfm>");
        output.Append(Encode(source)).Append("</div>");
    }

    private static string Excerpt(string body)
    {
        body = body.Replace("\r\n", "\n");
        var cut = body.IndexOf("\n\n", StringComparison.Ordinal);
        return cut < 0 ? body : body[..cut];
    }

    private static string Dedent(string text)
    {
        var lines = text.Replace("\r\n", "\n").Trim('\n').Split('\n');
        var indent = lines.Where(line => line.Trim().Length > 0)
            .Select(line => line.Length - line.TrimStart().Length)
            .DefaultIfEmpty(0)
            .Min();
        return string.Join('\n', lines.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart())).TrimEnd();
    }
}
