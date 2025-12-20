using Npgsql;

namespace AuthService.Infrastructure.Postgres.Options;

public sealed class DatabaseOptions
{
    public const string SECTION_NAME = "Database";

    public required string Host { get; init; }

    public int Port { get; init; } = 5432;

    public required string Database { get; init; }

    public required string Username { get; init; }

    public required string Password { get; init; }

    public string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password
        };

        return builder.ToString();
    }
}