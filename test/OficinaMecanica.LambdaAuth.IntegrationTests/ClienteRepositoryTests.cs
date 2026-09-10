namespace OficinaMecanica.LambdaAuth.IntegrationTests;

/// <summary>
/// Exercita ClienteRepository contra um PostgreSQL real com a copia do schema da aplicacao
/// (ADR-002 do repositorio oficina-mecanica-v2): uma renomeacao de coluna aqui deve quebrar
/// este teste em vez do login em producao.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ClienteRepositoryTests
{
    private const string CpfAtivo = "12345678909";
    private const string CpfInativo = "11144477735";
    private const string CpfInexistente = "11122233396";

    private readonly PostgresFixture _fixture;

    public ClienteRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task BuscarPorCpf_ComClienteAtivo_DeveRetornarClienteAtivo()
    {
        var id = Guid.NewGuid();
        await _fixture.InserirClienteAsync(id, CpfAtivo, ativo: true);

        var repository = new ClienteRepository(_fixture.ConnectionString);
        var cliente = await repository.BuscarPorCpfAsync(CpfAtivo, CancellationToken.None);

        Assert.NotNull(cliente);
        Assert.Equal(id, cliente!.Id);
        Assert.True(cliente.Ativo);
    }

    [Fact]
    public async Task BuscarPorCpf_ComClienteInativo_DeveRetornarClienteMarcadoComoInativo()
    {
        var id = Guid.NewGuid();
        await _fixture.InserirClienteAsync(id, CpfInativo, ativo: false);

        var repository = new ClienteRepository(_fixture.ConnectionString);
        var cliente = await repository.BuscarPorCpfAsync(CpfInativo, CancellationToken.None);

        Assert.NotNull(cliente);
        Assert.False(cliente!.Ativo);
    }

    [Fact]
    public async Task BuscarPorCpf_ComClienteInexistente_DeveRetornarNulo()
    {
        var repository = new ClienteRepository(_fixture.ConnectionString);
        var cliente = await repository.BuscarPorCpfAsync(CpfInexistente, CancellationToken.None);

        Assert.Null(cliente);
    }
}
