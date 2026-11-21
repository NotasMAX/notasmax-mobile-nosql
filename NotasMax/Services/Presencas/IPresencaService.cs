using NotasMax.Models.Presencas;

namespace NotasMax.Services.Presencas;

public interface IPresencaService
{
    Task<ResultadoPresenca> RegistrarPresenca(string tokenQr);
}