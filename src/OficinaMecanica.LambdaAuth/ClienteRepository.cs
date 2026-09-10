using Npgsql;

namespace OficinaMecanica.LambdaAuth;

/// <summary>
/// Le diretamente a tabela cadastro.cliente da aplicacao (ADR-002 do repositorio oficina-mecanica-v2).
/// Usa apenas SELECT, com um usuario de banco dedicado e somente-leitura.
/// </summary>
public sealed class ClienteRepository : IClienteRepository
{
    private const string Sql =
        "SELECT id, documento, ativo FROM cadastro.cliente WHERE documento = @documento LIMIT 1";

    private readonly string _connectionString;

    public ClienteRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Cliente?> BuscarPorCpfAsync(string cpfDigits, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("documento", cpfDigits);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new Cliente(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetBoolean(2));
    }
}
