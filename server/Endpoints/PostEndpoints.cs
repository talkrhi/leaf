using System.Security.Claims;
using System.Text.RegularExpressions;
using Leaf.Auth;
using Leaf.Data;

namespace Leaf.Endpoints;

public static partial class PostEndpoints
{
    public record PostInput(string? Title, string? Slug, string? Body, string? Tags, bool Published);
    public record CommentInput(string? Body);

    public static void MapPostEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/posts").RequireAuthorization("admin");

        admin.MapGet("/", (PostStore posts) => posts.List(includeDrafts: true));

        admin.MapGet("/{id:long}", (long id, PostStore posts) =>
            posts.Get(id) is { } post ? Results.Ok(post) : Results.NotFound());

        admin.MapPost("/", (PostInput input, ClaimsPrincipal user, PostStore posts) =>
            Save(new Post { AuthorId = user.UserId() }, input, posts));

        admin.MapPut("/{id:long}", (long id, PostInput input, PostStore posts) =>
            posts.Get(id) is { } post ? Save(post, input, posts) : Results.NotFound());

        admin.MapDelete("/{id:long}", (long id, PostStore posts) =>
        {
            posts.Delete(id);
            return Results.NoContent();
        });

        app.MapPost("/api/posts/{id:long}/comments",
            (long id, CommentInput input, ClaimsPrincipal user, PostStore posts, CommentStore comments, SettingsStore settings) =>
            {
                var body = input.Body?.Trim() ?? "";
                if (!settings.Get().AllowComments) return Api.Error("Comments are turned off.", StatusCodes.Status403Forbidden);
                if (body.Length is 0 or > 2000) return Api.Error("Comments must be between 1 and 2000 characters.");
                if (posts.Get(id) is not { Published: true }) return Results.NotFound();

                comments.Add(id, user.UserId()!.Value, body);
                return Results.Ok();
            }).RequireAuthorization("member");

        app.MapDelete("/api/comments/{id:long}", (long id, ClaimsPrincipal user, CommentStore comments) =>
        {
            var comment = comments.Get(id);
            if (comment is null) return Results.NotFound();
            if (comment.UserId != user.UserId() && !user.IsInRole("admin")) return Results.Forbid();

            comments.Delete(id);
            return Results.NoContent();
        }).RequireAuthorization("member");
    }

    private static IResult Save(Post post, PostInput input, PostStore posts)
    {
        var title = input.Title?.Trim() ?? "";
        if (title.Length == 0) return Api.Error("Posts need a title.");

        post.Title = title;
        post.Body = input.Body ?? "";
        post.Published = input.Published;
        post.Tags = string.Join(',', (input.Tags ?? "")
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => NotTagChars().Replace(tag.ToLowerInvariant(), ""))
            .Where(tag => tag.Length > 0)
            .Distinct());
        post.Slug = UniqueSlug(Slugify(string.IsNullOrWhiteSpace(input.Slug) ? title : input.Slug), post.Id, posts);

        var id = posts.Save(post);
        return Results.Ok(posts.Get(id));
    }

    private static string Slugify(string text)
    {
        var slug = NotSlugChars().Replace(text.ToLowerInvariant(), "-").Trim('-');
        if (slug.Length > 80) slug = slug[..80].Trim('-');
        return slug.Length > 0 ? slug : "post";
    }

    private static string UniqueSlug(string slug, long postId, PostStore posts)
    {
        var candidate = slug;
        for (var n = 2; posts.GetBySlug(candidate) is { } other && other.Id != postId; n++)
            candidate = $"{slug}-{n}";
        return candidate;
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NotSlugChars();

    [GeneratedRegex(@"[^\p{L}\p{N}_-]")]
    private static partial Regex NotTagChars();
}
