using System.Net;

namespace OficinaMecanica.LambdaAuth.UnitTests;

/// <summary>
/// Fluxo completo (repositorio HTTP real, com o Cadastro simulado + emissao de token) para os tres
/// caminhos exigidos pelo RFC-001: ativo emite token; inativo e inexistente falham com o mesmo
/// motivo generico.
/// </summary>
public class AuthenticationServiceComCadastroHttpTests
{
    private const string CpfValido = "22233344405";

    private static AuthenticationService BuildService(StubHttpMessageHandler cadastro)
    {
        var tokenFactory = new JwtTokenFactory(
            "segredo-de-teste-com-tamanho-suficiente-para-hs256", "oficina-mecanica-auth", "oficina-mecanica-api");
        return new AuthenticationService(
            new ClienteHttpRepository(StubHttpMessageHandler.ClienteHttp(cadastro), tokenFactory),
            tokenFactory);
    }

    [Fact]
    public async Task Autenticar_ComClienteAtivoNoCadastro_DeveEmitirToken()
    {
        var cadastro = StubHttpMessageHandler.ClienteEncontrado(Guid.NewGuid(), CpfValido, ativo: true);

        var resultado = await BuildService(cadastro).AutenticarAsync(CpfValido, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
    }

    [Fact]
    public async Task Autenticar_ComClienteInativoNoCadastro_DeveFalharComMotivoGenerico()
    {
        var cadastro = StubHttpMessageHandler.ClienteEncontrado(Guid.NewGuid(), CpfValido, ativo: false);

        var resultado = await BuildService(cadastro).AutenticarAsync(CpfValido, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.NaoAutenticado, resultado.FailureReason);
    }

    [Fact]
    public async Task Autenticar_ComClienteInexistenteNoCadastro_DeveFalharComOMesmoMotivoGenericoDoInativo()
    {
        var cadastro = StubHttpMessageHandler.Responde(HttpStatusCode.NotFound);

        var resultado = await BuildService(cadastro).AutenticarAsync(CpfValido, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal(AuthenticationFailureReason.NaoAutenticado, resultado.FailureReason);
    }

    [Fact]
    public async Task Autenticar_ComCpfInvalido_NaoDeveChamarOCadastro()
    {
        var cadastro = StubHttpMessageHandler.Responde(HttpStatusCode.NotFound);

        var resultado = await BuildService(cadastro).AutenticarAsync("111.111.111-11", CancellationToken.None);

        Assert.Equal(AuthenticationFailureReason.CpfInvalido, resultado.FailureReason);
        Assert.Equal(0, cadastro.ChamadasRecebidas);
    }
}
