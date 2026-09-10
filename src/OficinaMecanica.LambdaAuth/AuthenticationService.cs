using DocumentValidator;

namespace OficinaMecanica.LambdaAuth;

public enum AuthenticationFailureReason
{
    CpfInvalido,
    NaoAutenticado
}

public sealed class AuthenticationResult
{
    public bool Success { get; }
    public string? Token { get; }
    public DateTime? ExpiraEmUtc { get; }
    public AuthenticationFailureReason? FailureReason { get; }

    private AuthenticationResult(bool success, string? token, DateTime? expiraEmUtc, AuthenticationFailureReason? failureReason)
    {
        Success = success;
        Token = token;
        ExpiraEmUtc = expiraEmUtc;
        FailureReason = failureReason;
    }

    public static AuthenticationResult Sucesso(string token, DateTime expiraEmUtc) =>
        new(true, token, expiraEmUtc, null);

    public static AuthenticationResult Falha(AuthenticationFailureReason reason) =>
        new(false, null, null, reason);
}

/// <summary>
/// Orquestra o fluxo de autenticacao por CPF: valida o documento, consulta o cliente e emite o
/// token. Inexistente e inativo resultam no mesmo AuthenticationFailureReason.NaoAutenticado —
/// de propósito, para não transformar o endpoint num verificador de cadastro (RFC-001).
/// </summary>
public sealed class AuthenticationService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly JwtTokenFactory _tokenFactory;

    public AuthenticationService(IClienteRepository clienteRepository, JwtTokenFactory tokenFactory)
    {
        _clienteRepository = clienteRepository;
        _tokenFactory = tokenFactory;
    }

    public async Task<AuthenticationResult> AutenticarAsync(string? cpfBruto, CancellationToken cancellationToken)
    {
        var digits = new string((cpfBruto ?? string.Empty).Where(char.IsDigit).ToArray());

        if (!CpfValidation.Validate(digits))
            return AuthenticationResult.Falha(AuthenticationFailureReason.CpfInvalido);

        var cliente = await _clienteRepository.BuscarPorCpfAsync(digits, cancellationToken);
        if (cliente is null || !cliente.Ativo)
            return AuthenticationResult.Falha(AuthenticationFailureReason.NaoAutenticado);

        var token = _tokenFactory.Gerar(cliente.Id, digits);
        return AuthenticationResult.Sucesso(token.Token, token.ExpiraEmUtc);
    }
}
