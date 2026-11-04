using Leaf.Auth;
using Leaf.Data;
using Leaf.Endpoints;
using Leaf.LeafScript;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.GetFullPath(builder.Configuration["Leaf:DataDir"] ?? "site", builder.Environment.ContentRootPath);
var themeDir = Path.Combine(AppContext.BaseDirectory, "Theme");
var emojiDir = Path.Combine(dataDir, "emojis");

builder.Services.AddSingleton(new Database(Path.Combine(dataDir, "leaf.db")));
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<PostStore>();
builder.Services.AddSingleton<CommentStore>();
builder.Services.AddSingleton<SettingsStore>();
builder.Services.AddSingleton(new EmojiStore(emojiDir));
builder.Services.AddSingleton(new LeafFiles(Path.Combine(dataDir, "pages"), themeDir));
builder.Services.AddSingleton<LeafEngine>();
builder.Services.AddLeafAuth();

var app = builder.Build();
app.Services.GetRequiredService<Database>().Migrate();

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(emojiDir), RequestPath = "/emojis" });
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSetupRedirect();

app.MapAuthEndpoints();
app.MapPostEndpoints();
app.MapUserEndpoints();
app.MapPageEndpoints();
app.MapSettingsEndpoints();
app.MapEmojiEndpoints();
app.MapSite();

app.Run();
