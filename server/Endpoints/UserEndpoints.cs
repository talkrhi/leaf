using System.Security.Claims;
using Leaf.Auth;
using Leaf.Data;

namespace Leaf.Endpoints;

public static class UserEndpoints
{
    public record NewUser(string? Username, string? Password, string? Role);
    public record UserUpdate(string? Role, string? Password);

    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/users").RequireAuthorization("admin");

        users.MapGet("/", (UserStore store) => store.List());

        users.MapPost("/", (NewUser input, UserStore store) =>
        {
            var role = input.Role ?? "user";
            if (!Accounts.Roles.Contains(role)) return Api.Error("Unknown role.");
            if (Accounts.Check(input.Username, input.Password) is { } error) return Api.Error(error);
            if (store.Find(input.Username!) is not null) return Api.Error("That username is taken.");

            return Results.Ok(store.Create(input.Username!, Accounts.Hash(input.Password!), role));
        });

        users.MapPut("/{id:long}", (long id, UserUpdate input, ClaimsPrincipal me, UserStore store) =>
        {
            if (store.Get(id) is null) return Results.NotFound();

            if (input.Role is not null)
            {
                if (!Accounts.Roles.Contains(input.Role)) return Api.Error("Unknown role.");
                if (id == me.UserId() && input.Role != "admin") return Api.Error("You can't remove your own admin role.");
                store.SetRole(id, input.Role);
            }

            if (input.Password is not null)
            {
                if (Accounts.CheckPassword(input.Password) is { } error) return Api.Error(error);
                store.SetPassword(id, Accounts.Hash(input.Password));
            }

            return Results.Ok(store.Get(id));
        });

        users.MapDelete("/{id:long}", (long id, ClaimsPrincipal me, UserStore store) =>
        {
            if (id == me.UserId()) return Api.Error("You can't delete your own account.");
            store.Delete(id);
            return Results.NoContent();
        });

        users.MapPost("/remove-guests", (UserStore store) =>
            Results.Ok(new { removed = store.RemoveIdleGuests() }));
    }
}
