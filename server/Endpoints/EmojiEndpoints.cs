using Leaf.Data;
using Microsoft.AspNetCore.Mvc;

namespace Leaf.Endpoints;

public static class EmojiEndpoints
{
    public static void MapEmojiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/emojis", (EmojiStore emojis) => emojis.List());

        var admin = app.MapGroup("/api/emojis").RequireAuthorization("admin");

        admin.MapPost("/", async ([FromForm] string? name, IFormFile? file, EmojiStore emojis) =>
        {
            name = name?.Trim().Trim(':').ToLowerInvariant() ?? "";
            if (!EmojiStore.IsValidName(name))
                return Api.Error("Emoji names are 1-32 lowercase letters, numbers, dashes or underscores.");
            if (emojis.Exists(name)) return Api.Error($"There's already an emoji called :{name}:.");
            if (file is null || file.Length == 0) return Api.Error("Choose an image to upload.");
            if (file.Length > EmojiStore.MaxSize) return Api.Error("Emojis must be 1 MB or smaller.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!EmojiStore.Extensions.Contains(extension)) return Api.Error("Emojis must be PNG, GIF, WebP or JPEG images.");

            await using var image = file.OpenReadStream();
            return Results.Ok(await emojis.AddAsync(name, extension, image));
        }).DisableAntiforgery();

        admin.MapDelete("/{name}", (string name, EmojiStore emojis) =>
        {
            emojis.Delete(name);
            return Results.NoContent();
        });
    }
}
