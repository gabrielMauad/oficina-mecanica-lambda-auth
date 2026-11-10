using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;

namespace OficinaMecanica.LambdaAuth.UnitTests;

/// <summary>
/// Exercita o repositorio que consulta o servico de Cadastro por HTTP, com um HttpMessageHandler
/// fake no lugar da rede.
/// </summary>
public class ClienteHttpRepositoryTests
{
    private const string Secret = "segredo-de-teste-com-tamanho-suficiente-para-hs256";
    private const string Issuer = "oficina-mecanica-auth";
    private const string Audience = "oficina-mecanica-api";
    private const string Cpf = "12345678909";

    private static ClienteHttpRepository Repositorio(StubHttpMessageHandler handler) =>
        new(StubHttpMessageHandler.ClienteHttp(handler), new JwtTokenFactory(Secret, Issuer, Audience));

    [Fact]
    public async Task BuscarPorCpf_ComClienteAtivo_DeveRetornarClienteAtivo()
    {
        var id = Guid.NewGuid();
        var handler = StubHttpMessageHandler.ClienteEncontrado(id, Cpf, ativo: true);

        var cliente = await Repositorio(handler).BuscarPorCpfAsync(Cpf, CancellationToken.None);

        Assert.NotNull(cliente);
        Assert.Equal(id, cliente!.Id);
        Assert.Equal(Cpf, cliente.Documento);
        Assert.True(cliente.Ativo);
    }

    [Fact]
    public async Task BuscarPorCpf_ComClienteInativo_DeveRetornarClienteMarcadoComoInativo()
    {
        var handler = StubHttpMessageHandler.ClienteEncontrado(Guid.NewGuid(), Cpf, ativo: false);

        var cliente = await Repositorio(handler).BuscarPorCpfAsync(Cpf, CancellationToken.None);

        Assert.NotNull(cliente);
        Assert.False(cliente!.Ativo);
    }

    [Fact]
    public async Task BuscarPorCpf_Com404_DeveRetornarNulo()
    {
        var handler = StubHttpMessageHandler.Responde(HttpStatusCode.NotFound);

        var cliente = await Repositorio(handler).BuscarPorCpfAsync(Cpf, CancellationToken.None);

        Assert.Null(cliente);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task BuscarPorCpf_ComFalhaDoServico_DevePropagarErroEmVezDeRetornarNulo(HttpStatusCode status)
    {
        var handler = StubHttpMessageHandler.Responde(status);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Repositorio(handler).BuscarPorCpfAsync(Cpf, CancellationToken.None));
    }

    [Fact]
    public async Task BuscarPorCpf_DeveChamarARotaPorDocumentoDoCadastroComGet()
    {
        var handler = StubHttpMessageHandler.ClienteEncontrado(Guid.NewGuid(), Cpf, ativo: true);

        await Repositorio(handler).BuscarPorCpfAsync(Cpf, CancellationToken.None);

        var requisicao = handler.UltimaRequisicao!;
        Assert.Equal(HttpMethod.Get, requisicao.Method);
        Assert.Equal($"http://cadastro.interno:30082/api/v1/clientes/documento/{Cpf}", requisicao.RequestUri!.ToString());
    }

    [Fact]
    public async Task BuscarPorCpf_DeveEnviarTokenDeServicoValidoNoHeaderAuthorization()
    {
        var handler = StubHttpMessageHandler.ClienteEncontrado(Guid.NewGuid(), Cpf, ativo: true);

        await Repositorio(handler).BuscarPorCpfAsync(Cpf, CancellationToken.None);

        var authorization = handler.UltimaRequisicao!.Headers.Authorization;
        Assert.NotNull(authorization);
        Assert.Equal("Bearer", authorization!.Scheme);

        // Mesma validacao que os servicos fazem: MapInboundClaims = false, RoleClaimType = "role".
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
            authorization.Parameter,
            new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                RoleClaimType = "role",
                NameClaimType = JwtRegisteredClaimNames.Sub
            },
            out var validado);

        Assert.True(principal.IsInRole("Oficina"));
        Assert.Equal("oficina-mecanica-lambda-auth", principal.Identity!.Name);
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)validado).Header.Alg);
    }
}
