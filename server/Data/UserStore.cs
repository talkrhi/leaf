using Dapper;

namespace Leaf.Data;

public sealed class UserStore(Database db)
{
    private const string Columns = "id, username, password_hash, role, created_at";
    private volatile bool _hasAdmin;

    public bool HasAdmin()
    {
        if (_hasAdmin) return true;
        using var conn = db.Open();
        return _hasAdmin = conn.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM users WHERE role = 'admin')");
    }

    public User? Get(long id)
    {
        using var conn = db.Open();
        return conn.QuerySingleOrDefault<User>($"SELECT {Columns} FROM users WHERE id = @id", new { id });
    }

    public User? Find(string username)
    {
        using var conn = db.Open();
        return conn.QuerySingleOrDefault<User>($"SELECT {Columns} FROM users WHERE username = @username", new { username });
    }

    public List<User> List()
    {
        using var conn = db.Open();
        return conn.Query<User>($"SELECT {Columns} FROM users ORDER BY role, username").AsList();
    }

    public User Create(string username, string? passwordHash, string role)
    {
        using var conn = db.Open();
        var id = conn.ExecuteScalar<long>("""
            INSERT INTO users (username, password_hash, role, created_at)
            VALUES (@username, @passwordHash, @role, @now)
            RETURNING id
            """, new { username, passwordHash, role, now = Database.Now() });
        return Get(id)!;
    }

    public void SetRole(long id, string role) =>
        Execute("UPDATE users SET role = @role WHERE id = @id", new { id, role });

    public void SetPassword(long id, string passwordHash) =>
        Execute("UPDATE users SET password_hash = @passwordHash WHERE id = @id", new { id, passwordHash });

    public void Delete(long id) =>
        Execute("DELETE FROM users WHERE id = @id", new { id });

    public int RemoveIdleGuests() =>
        Execute("DELETE FROM users WHERE role = 'guest' AND id NOT IN (SELECT user_id FROM comments)");

    private int Execute(string sql, object? args = null)
    {
        using var conn = db.Open();
        return conn.Execute(sql, args);
    }
}
