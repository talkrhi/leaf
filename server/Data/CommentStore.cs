using Dapper;

namespace Leaf.Data;

public sealed class CommentStore(Database db)
{
    private const string Select = """
        SELECT c.id, c.post_id, c.user_id, u.username, u.role, c.body, c.created_at
        FROM comments c JOIN users u ON u.id = c.user_id
        """;

    public List<Comment> ForPost(long postId)
    {
        using var conn = db.Open();
        return conn.Query<Comment>($"{Select} WHERE c.post_id = @postId ORDER BY c.created_at", new { postId }).AsList();
    }

    public Comment? Get(long id)
    {
        using var conn = db.Open();
        return conn.QuerySingleOrDefault<Comment>($"{Select} WHERE c.id = @id", new { id });
    }

    public void Add(long postId, long userId, string body)
    {
        using var conn = db.Open();
        conn.Execute("""
            INSERT INTO comments (post_id, user_id, body, created_at)
            VALUES (@postId, @userId, @body, @now)
            """, new { postId, userId, body, now = Database.Now() });
    }

    public void Delete(long id)
    {
        using var conn = db.Open();
        conn.Execute("DELETE FROM comments WHERE id = @id", new { id });
    }
}
