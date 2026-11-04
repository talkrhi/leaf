using System.Security.Claims;
using Leaf.Data;

namespace Leaf.LeafScript;

public sealed class LeafContext(HttpContext http, SiteSettings settings)
{
    public HttpContext Http { get; } = http;
    public SiteSettings Settings { get; } = settings;
    public Dictionary<string, string> Vars { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Post? Post { get; init; }
    public string Slot { get; set; } = "";
    public int Depth { get; set; }

    public ClaimsPrincipal User => Http.User;
    public bool SignedIn => User.Identity?.IsAuthenticated == true;
    public bool IsAdmin => User.IsInRole("admin");

    public string Var(string name) => Vars.GetValueOrDefault(name) ?? "";
}
