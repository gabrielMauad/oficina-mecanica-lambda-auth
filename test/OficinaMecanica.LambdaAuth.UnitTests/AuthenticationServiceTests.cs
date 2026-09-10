namespace OficinaMecanica.LambdaAuth.UnitTests;

public class AuthenticationServiceTests
{
    private const string Secret = "segredo-de-teste-com-tamanho-suficiente-para-hs256";
    private const string CpfValido = "12345678909";

    private static JwtTokenFactory TokenFactory() =>
        new(Secret, "oficina-mecanica-auth", "oficina-mecanica-api");

    [Fact]
    public async Task Autenticar_ComCpfInvalido_DeveFalharSemConsultarRepositorio()
    {
        var repository = FakeClienteRepository.SemCliente();
        var service = new AuthenticationService(repository, TokenFactory());

        var resultado = await service.AutenticarAsync("111.111.111-11", CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.CpfInvalido, resultado.FailureReason);
        Assert.Equal(0, repository.ChamadasRecebidas);
    }

    [Fact]
    public async Task Autenticar_ComClienteInexistente_DeveFalharComMotivoGenerico()
    {
        var repository = FakeClienteRepository.SemCliente();
        var service = new AuthenticationService(repository, TokenFactory());

        var resultado = await service.AutenticarAsync(CpfValido, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.NaoAutenticado, resultado.FailureReason);
    }

    [Fact]
    public async Task Autenticar_ComClienteInativo_DeveFalharComOMesmoMotivoGenericoDoInexistente()
    {
        var repository = FakeClienteRepository.ComCliente(Guid.NewGuid(), CpfValido, ativo: false);
        var service = new AuthenticationService(repository, TokenFactory());

        var resultado = await service.AutenticarAsync(CpfValido, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.NaoAutenticado, resultado.FailureReason);
    }

    [Fact]
    public async Task Autenticar_ComClienteAtivo_DeveGerarToken()
    {
        var clienteId = Guid.NewGuid();
        var repository = FakeClienteRepository.ComCliente(clienteId, CpfValido, ativo: true);
        var service = new AuthenticationService(repository, TokenFactory());

        var resultado = await service.AutenticarAsync(CpfValido, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
        Assert.Equal(CpfValido, repository.UltimoDocumentoConsultado);
    }

    [Fact]
    public async Task Autenticar_DeveExtrairApenasDigitosAntesDeConsultarRepositorio()
    {
        var repository = FakeClienteRepository.ComCliente(Guid.NewGuid(), CpfValido, ativo: true);
        var service = new AuthenticationService(repository, TokenFactory());

        await service.AutenticarAsync("123.456.789-09", CancellationToken.None);

        Assert.Equal(CpfValido, repository.UltimoDocumentoConsultado);
    }
}
