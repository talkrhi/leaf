using System.Text.Json.Serialization;

namespace Leaf.Data;

public sealed class User
{

	public long Id { get; set; }
	public string Username {get; set; } = "";
	[JsonIgnore] public string? PasswordHash { get; set;}
	public string Role { get; set; } = "user";
	public string CreatedAt { get; set; } = "";
}

public sealed class Post
{

	public long Id { get; set; }
	public string Slug { get; set; } = "";
	public string Title { get; set; } = "";
	public string Body { get; set; } = "";
	public string Tags { get; set; } = "";
	public bool Published { get; set; }
	public long? AuthorId { get; set; }
	public string AuthorName { get; set; } = "";
	public string CreatedAt { get; set; } = "";
	public string UpdatedAt { get; set; } = "";

	public string [] TagList => Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
}

public sealed class Comment
{

	public long Id  { get; set; }
	public long PostId { get; set; }
	public long UserId { get; set; }
	public string Username { get; set; } = "";
	public string Role { get; set; } = "";
	public string Body { get; set; } = "";
	public string CreatedAt { get; set; } = "";
}

public sealed class Emoji
{

	public string Name { get; set; } = "";
	public string Url { get; set; } = "";
}

public sealed class SiteSettings
{

	public string Title { get; set; } = "This is your Leaf blog";
	public string Description { get; set; } = "A simple blog powered by Leaf.";
public bool AllowRegistration { get; set; }
public bool AllowGuests { get; set; } = true;
public bool AllowComments { get; set; } = true;
}
