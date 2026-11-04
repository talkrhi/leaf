using Leaf.LeafScript;

namespace Leaf.Endpoints;

public static class PageEndpoints
{
    public record PageSource(string? Source);

    private static readonly string[] Required = ["_layout", "index", "post", "login", "setup", "404"];

    public static void MapPageEndpoints(this WebApplication app)
    {
        var pages = app.MapGroup("/api/pages").RequireAuthorization("admin");

        pages.MapGet("/", (LeafFiles files) =>
            files.Names().Select(name => new { name, routable = !name.StartsWith('_') }));

        pages.MapGet("/{name}", (string name, LeafFiles files) =>
            files.Exists(name) ? Results.Ok(new { name, source = files.Read(name) }) : Results.NotFound());

        pages.MapPut("/{name}", (string name, PageSource input, LeafFiles files) =>
        {
            if (!LeafFiles.IsValidName(name))
                return Api.Error("Page names use lowercase letters, numbers and dashes. Start with _ for a partial.");
            try
            {
                files.Write(name, input.Source ?? "");
                return Results.Ok();
            }
            catch (LeafException ex)
            {
                return Api.Error(ex.Message);
            }
        });

        pages.MapDelete("/{name}", (string name, LeafFiles files) =>
        {
            if (Required.Contains(name)) return Api.Error("Leaf needs this page, so it can't be deleted.");
            if (files.Exists(name)) files.Delete(name);
            return Results.NoContent();
        });
    }
}
