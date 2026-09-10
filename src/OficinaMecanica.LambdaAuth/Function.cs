using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OficinaMecanica.LambdaAuth;

/// <summary>
/// Handler da Function de autenticacao por CPF, atras de um API Gateway HTTP API (payload v2).
/// Contrato de resposta: 200 com o token em caso de sucesso; 400 para CPF invalido; 401 generico
/// para cliente inexistente ou inativo (RFC-001 — nao revelar qual dos dois casos ocorreu).
/// </summary>
public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuthenticationService _authenticationService;

    public Function() : this(BuildAuthenticationService())
    {
    }

    internal Function(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AutenticarCpfRequest? payload;
        try
        {
            payload = string.IsNullOrWhiteSpace(request.Body)
                ? null
                : JsonSerializer.Deserialize<AutenticarCpfRequest>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return ErroResponse(400, "requisicao_invalida", "Corpo da requisicao invalido.");
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Cpf))
            return ErroResponse(400, "cpf_invalido", "CPF e obrigatorio.");

        var resultado = await _authenticationService.AutenticarAsync(payload.Cpf, CancellationToken.None);

        if (resultado.Success)
        {
            var expiresIn = (int)resultado.ExpiraEmUtc!.Value.Subtract(DateTime.UtcNow).TotalSeconds;
            return JsonResponse(200, new AutenticarCpfResponse
            {
                Token = resultado.Token!,
                ExpiresIn = Math.Max(expiresIn, 0)
            });
        }

        return resultado.FailureReason switch
        {
            AuthenticationFailureReason.CpfInvalido =>
                ErroResponse(400, "cpf_invalido", "CPF invalido."),
            _ =>
                ErroResponse(401, "nao_autenticado", "Nao foi possivel autenticar com o CPF informado.")
        };
    }

    private static APIGatewayHttpApiV2ProxyResponse ErroResponse(int statusCode, string erro, string mensagem) =>
        JsonResponse(statusCode, new ErroResponse { Erro = erro, Mensagem = mensagem });

    private static APIGatewayHttpApiV2ProxyResponse JsonResponse(int statusCode, object body) =>
        new()
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body, JsonOptions),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };

    private static AuthenticationService BuildAuthenticationService()
    {
        var connectionString = RequireEnv("DB_CONNECTION_STRING");
        var secret = RequireEnv("JWT_SECRET");
        var issuer = RequireEnv("JWT_ISSUER");
        var audience = RequireEnv("JWT_AUDIENCE");

        var repository = new ClienteRepository(connectionString);
        var tokenFactory = new JwtTokenFactory(secret, issuer, audience);
        return new AuthenticationService(repository, tokenFactory);
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Variavel de ambiente {name} nao configurada.");
}
