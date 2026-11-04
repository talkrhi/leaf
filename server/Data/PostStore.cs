using Dapper;

namespace Leaf.Data;

public sealed class PostStore(Database db)
{
    private const string Select = """
        SELECT p.id, p.slug, p.title, p.body, p.tags, p.published, p.author_id,
               COALESCE(u.username, 'deleted') AS author_name, p.created_at, p.updated_at
        FROM posts p LEFT JOIN users u ON u.id = p.author_id
        """;

    public List<Post> List(bool includeDrafts, string? tag = null, int skip = 0, int take = 1000)
    {
        using var conn = db.Open();
        return conn.Query<Post>($"""
            {Select}
            WHERE (@includeDrafts OR p.published = 1)
              AND (@tag IS NULL OR instr(',' || p.tags || ',', ',' || @tag || ',') > 0)
            ORDER BY p.created_at DESC
            LIMIT @take OFFSET @skip
            """, new { includeDrafts, tag = string.IsNullOrEmpty(tag) ? null : tag, skip, take }).AsList();
    }

    public Post? Get(long id)
    {
        using var conn = db.Open();
        return conn.QuerySingleOrDefault<Post>($"{Select} WHERE p.id = @id", new { id });
    }

    public Post? GetBySlug(string slug)
    {
        using var conn = db.Open();
        return conn.QuerySingleOrDefault<Post>($"{Select} WHERE p.slug = @slug", new { slug });
    }

    public long Save(Post post)
    {
        using var conn = db.Open();
        var now = Database.Now();
        if (post.Id == 0)
        {
            return conn.ExecuteScalar<long>("""
                INSERT INTO posts (slug, title, body, tags, published, author_id, created_at, updated_at)
                VALUES (@Slug, @Title, @Body, @Tags, @Published, @AuthorId, @now, @now)
                RETURNING id
                """, new { post.Slug, post.Title, post.Body, post.Tags, post.Published, post.AuthorId, now });
        }
        conn.Execute("""
            UPDATE posts
            SET slug = @Slug, title = @Title, body = @Body, tags = @Tags, published = @Published, updated_at = @now
            WHERE id = @Id
            """, new { post.Slug, post.Title, post.Body, post.Tags, post.Published, post.Id, now });
        return post.Id;
    }

    public void Delete(long id)
    {
        using var conn = db.Open();
        conn.Execute("DELETE FROM posts WHERE id = @id", new { id });
    }
}
