using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace OficinaMecanica.LambdaAuth.UnitTests;

/// <summary>
/// Confere o token emitido contra o contrato do RFC-001 §4.1 (repositorio oficina-mecanica-v2):
/// iss, aud, sub, role, cpf, exp e algoritmo HS256, com nomes curtos de claim ("sub", "role").
/// </summary>
public class JwtTokenFactoryTests
{
    private const string Secret = "segredo-de-teste-com-tamanho-suficiente-para-hs256";
    private const string Issuer = "oficina-mecanica-auth";
    private const string Audience = "oficina-mecanica-api";

    private static JwtSecurityToken GerarToken(Guid clienteId, string cpf, out DateTime expiraEmUtc)
    {
        var factory = new JwtTokenFactory(Secret, Issuer, Audience);
        var resultado = factory.Gerar(clienteId, cpf);
        expiraEmUtc = resultado.ExpiraEmUtc;
        return new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);
    }

    [Fact]
    public void Gerar_DeveEmitirTokenComIssuerDoContrato()
    {
        var token = GerarToken(Guid.NewGuid(), "12345678909", out _);
        Assert.Equal(Issuer, token.Issuer);
    }

    [Fact]
    public void Gerar_DeveEmitirTokenComAudienceDoContrato()
    {
        var token = GerarToken(Guid.NewGuid(), "12345678909", out _);
        Assert.Contains(Audience, token.Audiences);
    }

    [Fact]
    public void Gerar_DeveUsarSubComOIdDoCliente()
    {
        var clienteId = Guid.NewGuid();
        var token = GerarToken(clienteId, "12345678909", out _);

        Assert.Equal(clienteId.ToString(), token.Subject);
    }

    [Fact]
    public void Gerar_DeveUsarClaimCurtaDeRoleComValorCliente()
    {
        var token = GerarToken(Guid.NewGuid(), "12345678909", out _);

        var roleClaim = token.Claims.Single(c => c.Type == "role");
        Assert.Equal("Cliente", roleClaim.Value);

        // A aplicacao usa MapInboundClaims = false / RoleClaimType = "role": a claim nao pode
        // sair como a URI longa de ClaimTypes.Role, senao a autorizacao por papel nao funciona.
        Assert.DoesNotContain(token.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role);
    }

    [Fact]
    public void Gerar_DeveIncluirClaimCpfComSomenteDigitos()
    {
        var token = GerarToken(Guid.NewGuid(), "12345678909", out _);

        var cpfClaim = token.Claims.Single(c => c.Type == "cpf");
        Assert.Equal("12345678909", cpfClaim.Value);
    }

    [Fact]
    public void Gerar_DeveExpirarEmUmaHora()
    {
        var antes = DateTime.UtcNow;
        var token = GerarToken(Guid.NewGuid(), "12345678909", out var expiraEmUtc);
        var depois = DateTime.UtcNow;

        Assert.True(token.ValidTo >= antes.AddHours(1).AddSeconds(-5));
        Assert.True(token.ValidTo <= depois.AddHours(1).AddSeconds(5));
        Assert.Equal(token.ValidTo, expiraEmUtc, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Gerar_DeveAssinarComHS256()
    {
        var token = GerarToken(Guid.NewGuid(), "12345678909", out _);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
    }

    [Fact]
    public void Gerar_DeveProduzirTokenValidavelComOMesmoSegredo()
    {
        var factory = new JwtTokenFactory(Secret, Issuer, Audience);
        var resultado = factory.Gerar(Guid.NewGuid(), "12345678909");

        // MapInboundClaims = false replica a configuracao da aplicacao: sem isso, o handler
        // remapeia "role" para a URI longa de ClaimTypes.Role e RoleClaimType = "role" nao acha nada.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Sub
        };

        var principal = handler.ValidateToken(resultado.Token, validationParameters, out _);
        Assert.Equal("Cliente", principal.FindFirst("role")!.Value);
    }
}
