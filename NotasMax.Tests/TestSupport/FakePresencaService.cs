using NotasMax.Models.Presencas;
using NotasMax.Services.Presencas;

namespace NotasMax.Tests.TestSupport;

public sealed class FakePresencaService : IPresencaService
{
    public ResultadoPresenca Resultado { get; set; } = new()
    {
        Status = StatusPresenca.Sucesso
    };

    public Exception? ExceptionToThrow { get; set; }
    public int CallCount { get; private set; }
    public List<string> TokensRecebidos { get; } = [];

    public Task<ResultadoPresenca> RegistrarPresenca(string tokenQr)
    {
        CallCount++;
        TokensRecebidos.Add(tokenQr);

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.FromResult(Resultado);
    }
}
