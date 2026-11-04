using Dapper;
using Microsoft.Data.Sqlite;

namespace Leaf.Data;

public sealed class Database
{
    private readonly string _connectionString;

    public Database(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = file, ForeignKeys = true }.ToString();
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public static string Now() => DateTime.UtcNow.ToString("o");

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Migrate()
    {
        using var conn = Open();
        conn.Execute(Schema);
    }

    private const string Schema = """
        PRAGMA journal_mode = WAL;

        CREATE TABLE IF NOT EXISTS users (
            id            INTEGER PRIMARY KEY,
            username      TEXT NOT NULL UNIQUE COLLATE NOCASE,
            password_hash TEXT,
            role          TEXT NOT NULL,
            created_at    TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS posts (
            id         INTEGER PRIMARY KEY,
            slug       TEXT NOT NULL UNIQUE,
            title      TEXT NOT NULL,
            body       TEXT NOT NULL,
            tags       TEXT NOT NULL DEFAULT '',
            published  INTEGER NOT NULL DEFAULT 0,
            author_id  INTEGER REFERENCES users(id) ON DELETE SET NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS comments (
            id         INTEGER PRIMARY KEY,
            post_id    INTEGER NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
            user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            body       TEXT NOT NULL,
            created_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS settings (
            key   TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );
        """;
}
