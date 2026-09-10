namespace OficinaMecanica.LambdaAuth.IntegrationTests;

/// <summary>
/// Fluxo completo (repositorio real + emissao de token) para os tres caminhos exigidos pelo
/// RFC-001: ativo emite token, inativo e inexistente falham com o mesmo motivo generico.
/// </summary>
[Collection(PostgresCollection.Name)]
public class AuthenticationServiceIntegrationTests
{
    // CPFs distintos dos usados em ClienteRepositoryTests: os dois conjuntos de testes
    // compartilham o mesmo container Postgres (mesma PostgresFixture na collection "postgres").
    private const string CpfAtivo = "22233344405";
    private const string CpfInativo = "33344455508";
    private const string CpfInexistente = "44455566619";

    private readonly PostgresFixture _fixture;

    public AuthenticationServiceIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

    private AuthenticationService BuildService() =>
        new(
            new ClienteRepository(_fixture.ConnectionString),
            new JwtTokenFactory("segredo-de-teste-com-tamanho-suficiente-para-hs256", "oficina-mecanica-auth", "oficina-mecanica-api"));

    [Fact]
    public async Task Autenticar_ComClienteAtivoNoBanco_DeveEmitirToken()
    {
        await _fixture.InserirClienteAsync(Guid.NewGuid(), CpfAtivo, ativo: true);

        var resultado = await BuildService().AutenticarAsync(CpfAtivo, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
    }

    [Fact]
    public async Task Autenticar_ComClienteInativoNoBanco_DeveFalharComMotivoGenerico()
    {
        await _fixture.InserirClienteAsync(Guid.NewGuid(), CpfInativo, ativo: false);

        var resultado = await BuildService().AutenticarAsync(CpfInativo, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.NaoAutenticado, resultado.FailureReason);
    }

    [Fact]
    public async Task Autenticar_ComClienteInexistenteNoBanco_DeveFalharComOMesmoMotivoGenericoDoInativo()
    {
        var resultado = await BuildService().AutenticarAsync(CpfInexistente, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.NaoAutenticado, resultado.FailureReason);
    }
}
