using Npgsql;
using Testcontainers.PostgreSql;

namespace OficinaMecanica.LambdaAuth.IntegrationTests;

/// <summary>
/// Sobe um PostgreSQL real via Testcontainers e aplica a copia do schema em
/// Schema/cadastro_cliente.sql (ver comentario no arquivo para a fonte de verdade).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("oficina_mecanica")
        .WithUsername("oficina")
        .WithPassword("oficina")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schema", "cadastro_cliente.sql");
        var schemaSql = await File.ReadAllTextAsync(schemaPath);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(schemaSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task InserirClienteAsync(Guid id, string documento, bool ativo)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO cadastro.cliente (id, nome, documento, email, telefone, ativo, created_at, updated_at)
            VALUES (@id, @nome, @documento, @email, @telefone, @ativo, now(), now())
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("nome", "Cliente de teste");
        command.Parameters.AddWithValue("documento", documento);
        command.Parameters.AddWithValue("email", "cliente-teste@example.com");
        command.Parameters.AddWithValue("telefone", "11999999999");
        command.Parameters.AddWithValue("ativo", ativo);

        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
