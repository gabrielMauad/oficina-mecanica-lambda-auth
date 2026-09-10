using DocumentValidator;

namespace OficinaMecanica.LambdaAuth.UnitTests;

/// <summary>
/// Exercita diretamente o pacote DocsBRValidator (DocumentValidator.CpfValidation), a mesma
/// biblioteca usada por Cadastro.Domain.Cliente.Cpf na aplicacao — garantindo que a Lambda
/// aplique exatamente a mesma regra de validacao de CPF.
/// </summary>
public class CpfValidationTests
{
    [Theory]
    [InlineData("12345678909")]
    [InlineData("11144477735")]
    public void Validate_DeveAceitar_CpfComDigitosVerificadoresCorretos(string cpf)
    {
        Assert.True(CpfValidation.Validate(cpf));
    }

    [Theory]
    [InlineData("12345678900")]
    [InlineData("11144477736")]
    public void Validate_DeveRejeitar_CpfComDigitoVerificadorErrado(string cpf)
    {
        Assert.False(CpfValidation.Validate(cpf));
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void Validate_DeveRejeitar_SequenciaDeDigitosRepetidos(string cpf)
    {
        Assert.False(CpfValidation.Validate(cpf));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("123456789091")]
    [InlineData("")]
    public void Validate_DeveRejeitar_QuantidadeDeDigitosInvalida(string cpf)
    {
        Assert.False(CpfValidation.Validate(cpf));
    }

    [Fact]
    public void ExtrairDigitos_DeveIgnorarPontuacao_AntesDeValidar()
    {
        var digits = new string("123.456.789-09".Where(char.IsDigit).ToArray());

        Assert.Equal("12345678909", digits);
        Assert.True(CpfValidation.Validate(digits));
    }
}
