namespace OficinaMecanica.LambdaAuth;

public interface IClienteRepository
{
    Task<Cliente?> BuscarPorCpfAsync(string cpfDigits, CancellationToken cancellationToken);
}
