using System.Security.Cryptography;
using Leaf.Auth;
using Leaf.Data;
using Microsoft.AspNetCore.Authentication;

namespace Leaf.Endpoints;

public static class AuthEndpoints
{
    public record Credentials(string? Username, string? Password);
    public record SetupRequest(string? SiteTitle, string? Username, string? Password);

    private static readonly Lock SetupLock = new();

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth");

        auth.MapPost("/setup", async (SetupRequest req, HttpContext http, UserStore users, SettingsStore settings) =>
        {
            if (Accounts.Check(req.Username, req.Password) is { } error) return Api.Error(error);

            User admin;
            lock (SetupLock)
            {
                if (users.HasAdmin()) return Api.Error("This blog is already set up.", StatusCodes.Status409Conflict);
                admin = users.Create(req.Username!, Accounts.Hash(req.Password!), "admin");
            }

            var site = settings.Get();
            if (!string.IsNullOrWhiteSpace(req.SiteTitle)) site.Title = req.SiteTitle.Trim();
            settings.Save(site);

            await AuthSetup.SignInAsync(http, admin);
            return Results.Ok();
        });

        auth.MapPost("/login", async (Credentials req, HttpContext http, UserStore users) =>
        {
            var user = users.Find(req.Username ?? "");
            if (user is null || !Accounts.Verify(user, req.Password ?? "")) return Api.Error("Wrong username or password.");

            await AuthSetup.SignInAsync(http, user);
            return Results.Ok();
        });

        auth.MapPost("/register", async (Credentials req, HttpContext http, UserStore users, SettingsStore settings) =>
        {
            if (!settings.Get().AllowRegistration) return Api.Error("Registration is closed.", StatusCodes.Status403Forbidden);
            if (Accounts.Check(req.Username, req.Password) is { } error) return Api.Error(error);
            if (users.Find(req.Username!) is not null) return Api.Error("That username is taken.");

            var user = users.Create(req.Username!, Accounts.Hash(req.Password!), "user");
            await AuthSetup.SignInAsync(http, user);
            return Results.Ok();
        });

        auth.MapPost("/guest", async (HttpContext http, UserStore users, SettingsStore settings) =>
        {
            if (!settings.Get().AllowGuests) return Api.Error("Guest access is turned off.", StatusCodes.Status403Forbidden);

            var name = "guest-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(3));
            var user = users.Create(name, passwordHash: null, "guest");
            await AuthSetup.SignInAsync(http, user);
            return Results.Ok();
        });

        auth.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync();
            return Results.Ok();
        });
    }
}
