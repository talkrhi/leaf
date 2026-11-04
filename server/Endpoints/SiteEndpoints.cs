using Leaf.Data;
using Leaf.LeafScript;

namespace Leaf.Endpoints;

public static class SiteEndpoints
{
    public static void UseSetupRedirect(this WebApplication app) =>
        app.Use(async (http, next) =>
        {
            var path = http.Request.Path;
            if (!path.StartsWithSegments("/api") && path != "/setup" &&
                !http.RequestServices.GetRequiredService<UserStore>().HasAdmin())
            {
                http.Response.Redirect("/setup");
                return;
            }
            await next(http);
        });

    public static void MapSite(this WebApplication app)
    {
        app.MapGet("/admin", () =>
            TypedResults.PhysicalFile(Path.Combine(AppContext.BaseDirectory, "Admin", "admin.html"), "text/html"))
            .RequireAuthorization("admin");

        app.MapGet("/{**path}", (string? path, HttpContext http, LeafEngine leaf, PostStore posts, UserStore users) =>
        {
            path = (path ?? "").Trim('/').ToLowerInvariant();
            if (path == "setup" && users.HasAdmin()) return Results.Redirect("/");

            if (path.StartsWith("post/"))
            {
                var post = posts.GetBySlug(path["post/".Length..]);
                var visible = post is not null && (post.Published || http.User.IsInRole("admin"));
                return visible ? leaf.Render(http, "post", post) : leaf.NotFound(http);
            }

            if (path == "") path = "index";
            return path != "post" && leaf.Files.IsRoutable(path) ? leaf.Render(http, path) : leaf.NotFound(http);
        });
    }
}
