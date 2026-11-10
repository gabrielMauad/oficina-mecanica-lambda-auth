using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OficinaMecanica.LambdaAuth;

/// <summary>
/// Consulta o servico de Cadastro por HTTP (GET /api/v1/clientes/documento/{cpf}). O Cadastro e o
/// unico dono dos dados de cliente e do seu banco: nenhum outro servico le esse banco. A rota
/// exige o papel Oficina, entao cada chamada leva um JWT de servico emitido pela propria Function.
/// 404 significa cliente inexistente; qualquer outra resposta fora de 2xx e falha de infraestrutura
/// e propaga como HttpRequestException (nao deve virar um 401 "cliente nao encontrado").
/// </summary>
public sealed class ClienteHttpRepository : IClienteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly JwtTokenFactory _tokenFactory;

    public ClienteHttpRepository(HttpClient httpClient, JwtTokenFactory tokenFactory)
    {
        _httpClient = httpClient;
        _tokenFactory = tokenFactory;
    }

    public async Task<Cliente?> BuscarPorCpfAsync(string cpfDigits, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"api/v1/clientes/documento/{Uri.EscapeDataString(cpfDigits)}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", _tokenFactory.GerarTokenDeServico().Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ClienteResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia do servico de Cadastro.");

        return new Cliente(body.Id, body.Documento ?? cpfDigits, body.Ativo);
    }

    private sealed record ClienteResponse(Guid Id, string? Documento, bool Ativo);
}
