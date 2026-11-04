using Leaf.Data;

namespace Leaf.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var settings = app.MapGroup("/api/settings").RequireAuthorization("admin");

        settings.MapGet("/", (SettingsStore store) => store.Get());

        settings.MapPut("/", (SiteSettings input, SettingsStore store) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title)) return Api.Error("Your blog needs a title.");
            input.Title = input.Title.Trim();
            input.Description = input.Description?.Trim() ?? "";
            store.Save(input);
            return Results.Ok(input);
        });
    }
}
