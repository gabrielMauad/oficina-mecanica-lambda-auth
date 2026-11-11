using System.Net;
using System.Text;

namespace OficinaMecanica.LambdaAuth.UnitTests;

/// <summary>Handler fake: devolve uma resposta fixa e guarda a requisicao recebida.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _jsonBody;

    public HttpRequestMessage? UltimaRequisicao { get; private set; }
    public int ChamadasRecebidas { get; private set; }

    private StubHttpMessageHandler(HttpStatusCode statusCode, string? jsonBody)
    {
        _statusCode = statusCode;
        _jsonBody = jsonBody;
    }

    public static StubHttpMessageHandler Responde(HttpStatusCode statusCode, string? jsonBody = null) =>
        new(statusCode, jsonBody);

    public static StubHttpMessageHandler ClienteEncontrado(Guid id, string documento, bool ativo) =>
        new(HttpStatusCode.OK,
            $$"""
            {"id":"{{id}}","nome":"Fulano","documento":"{{documento}}","email":"fulano@exemplo.com","telefone":"11999999999","ativo":{{(ativo ? "true" : "false")}},"cadastradoEm":"2026-01-01T00:00:00Z","atualizadoEm":"2026-01-01T00:00:00Z"}
            """);

    public static HttpClient ClienteHttp(StubHttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://cadastro.interno:30082/") };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ChamadasRecebidas++;
        UltimaRequisicao = request;

        var response = new HttpResponseMessage(_statusCode);
        if (_jsonBody is not null)
            response.Content = new StringContent(_jsonBody, Encoding.UTF8, "application/json");
        return Task.FromResult(response);
    }
}
