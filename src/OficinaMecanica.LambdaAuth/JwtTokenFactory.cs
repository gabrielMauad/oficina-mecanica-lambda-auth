using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OficinaMecanica.LambdaAuth;

/// <summary>
/// Emite o token do cliente conforme o contrato do RFC-001 (secao 4.1) do repositorio
/// oficina-mecanica-v2: HS256, segredo simetrico compartilhado com a aplicacao, claims
/// curtas ("sub", "role") para casar com MapInboundClaims = false / RoleClaimType = "role"
/// na aplicacao.
/// </summary>
public sealed class JwtTokenFactory
{
    private static readonly TimeSpan Expiracao = TimeSpan.FromHours(1);

    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenFactory(string secret, string issuer, string audience)
    {
        _secret = secret;
        _issuer = issuer;
        _audience = audience;
    }

    public TokenGerado Gerar(Guid clienteId, string cpfDigits)
    {
        var expiresAt = DateTime.UtcNow.Add(Expiracao);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
            new Claim("role", "Cliente"),
            new Claim("cpf", cpfDigits)
        };

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
