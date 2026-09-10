namespace OficinaMecanica.LambdaAuth.UnitTests;

internal sealed class FakeClienteRepository : IClienteRepository
{
    private readonly Cliente? _cliente;

    public int ChamadasRecebidas { get; private set; }
    public string? UltimoDocumentoConsultado { get; private set; }

    private FakeClienteRepository(Cliente? cliente) => _cliente = cliente;

    public static FakeClienteRepository SemCliente() => new(null);

    public static FakeClienteRepository ComCliente(Guid id, string documento, bool ativo) =>
        new(new Cliente(id, documento, ativo));

    public Task<Cliente?> BuscarPorCpfAsync(string cpfDigits, CancellationToken cancellationToken)
    {
        ChamadasRecebidas++;
        UltimoDocumentoConsultado = cpfDigits;
        return Task.FromResult(_cliente);
    }
}
