using System.Text.RegularExpressions;
using Leaf.Data;
using Microsoft.AspNetCore.Identity;

namespace Leaf.Auth;

public static partial class Accounts
{
    public static readonly string[] Roles = ["admin", "user", "guest"];

    private static readonly PasswordHasher<User> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(new User(), password);

    public static bool Verify(User user, string password) =>
        user.PasswordHash is not null &&
        Hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    [GeneratedRegex("^[A-Za-z0-9_]{3,24}$")]
    private static partial Regex UsernamePattern();

    public static string? CheckUsername(string? username) =>
        username is not null && UsernamePattern().IsMatch(username)
            ? null
            : "Usernames are 3-24 letters, numbers or underscores.";

    public static string? CheckPassword(string? password) =>
        password is { Length: >= 8 and <= 256 } ? null : "Passwords need at least 8 characters.";

    public static string? Check(string? username, string? password) =>
        CheckUsername(username) ?? CheckPassword(password);
}
