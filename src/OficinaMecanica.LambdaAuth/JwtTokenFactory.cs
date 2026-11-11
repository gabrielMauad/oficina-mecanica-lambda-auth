using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OficinaMecanica.LambdaAuth;

/// <summary>
/// Emite os tokens da Function, ambos HS256 com o segredo simetrico compartilhado com os
/// servicos (ADR-001 do repositorio oficina-mecanica-app) e claims curtas ("sub", "role") para
/// casar com MapInboundClaims = false / RoleClaimType = "role" nos servicos:
/// - token do cliente (RFC-001 secao 4.1): role=Cliente, valido por 1 hora;
/// - token de servico, usado pela propria Function para consultar o Cadastro: role=Oficina,
///   valido por 5 minutos.
/// </summary>
public sealed class JwtTokenFactory
{
    public const string SubjectDoServico = "oficina-mecanica-lambda-auth";

    private static readonly TimeSpan ExpiracaoCliente = TimeSpan.FromHours(1);
    private static readonly TimeSpan ExpiracaoServico = TimeSpan.FromMinutes(5);

    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenFactory(string secret, string issuer, string audience)
    {
        _secret = secret;
        _issuer = issuer;
        _audience = audience;
    }

    public TokenGerado Gerar(Guid clienteId, string cpfDigits) =>
        Assinar(
            ExpiracaoCliente,
            new Claim(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
            new Claim("role", "Cliente"),
            new Claim("cpf", cpfDigits));

    public TokenGerado GerarTokenDeServico() =>
        Assinar(
            ExpiracaoServico,
            new Claim(JwtRegisteredClaimNames.Sub, SubjectDoServico),
            new Claim("role", "Oficina"));

    private TokenGerado Assinar(TimeSpan expiracao, params Claim[] claims)
    {
        var expiresAt = DateTime.UtcNow.Add(expiracao);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenGerado(tokenString, expiresAt);
    }
}

public sealed record TokenGerado(string Token, DateTime ExpiraEmUtc);
