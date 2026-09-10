namespace OficinaMecanica.LambdaAuth;

public sealed class AutenticarCpfRequest
{
    public string? Cpf { get; set; }
}

public sealed class AutenticarCpfResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}

public sealed class ErroResponse
{
    public string Erro { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
}
