using System.Net;
using NotasMax.Models.Presencas;
using NotasMax.Services.RequestProvider;
using NotasMax.Services.Settings;

namespace NotasMax.Services.Presencas;

public class PresencaService(IRequestProvider requestProvider, ISettingsService settingsService) : IPresencaService
{
    private readonly IRequestProvider _requestProvider = requestProvider;
    private readonly ISettingsService _settingsService = settingsService;

    public async Task<ResultadoPresenca> RegistrarPresenca(string tokenQr)
    {
        var tokenSessao = _settingsService.AuthAccessToken;
        if (string.IsNullOrWhiteSpace(tokenSessao))
        {
            return new ResultadoPresenca
            {
                Status = StatusPresenca.SessaoExpirada,
                Mensagem = "Sua sessão expirou.",
                StatusHttp = HttpStatusCode.Unauthorized
            };
        }

        var uri = $"{GlobalSettings.DefaultEndpoint}/Presenca/Registrar";

        try
        {
            var (statusCode, data) = await _requestProvider.PostWithStatusAsync<RegistrarPresencaResponse, RegistrarPresencaRequest>(
                uri,
                new RegistrarPresencaRequest { Token = tokenQr },
                tokenSessao);

            var mensagemApi = data?.Message?.Trim() ?? string.Empty;

            if (statusCode == HttpStatusCode.Created)
            {
                return new ResultadoPresenca
                {
                    Status = StatusPresenca.Sucesso,
                    Mensagem = string.IsNullOrWhiteSpace(mensagemApi) ? "Presença registrada!" : mensagemApi,
                    StatusHttp = statusCode,
                    Simulado = data?.Simulado
                };
            }

            if (statusCode == HttpStatusCode.Conflict)
            {
                return CriarResultado(StatusPresenca.JaRegistrada, "Você já registrou presença neste simulado.", statusCode);
            }

            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return CriarResultado(StatusPresenca.SessaoExpirada, mensagemApi, statusCode);
            }

            if (statusCode == HttpStatusCode.Forbidden)
            {
                if (mensagemApi.Contains("turma", StringComparison.OrdinalIgnoreCase))
                    return CriarResultado(StatusPresenca.TurmaDiferente,
                        "Este QR Code não faz parte da sua turma.", statusCode);

                if (mensagemApi.Contains("sess", StringComparison.OrdinalIgnoreCase))
                    return CriarResultado(StatusPresenca.SessaoExpirada, mensagemApi, statusCode);

                return CriarResultado(StatusPresenca.QrInvalido,
                    string.IsNullOrWhiteSpace(mensagemApi) ? "QR Code inválido" : mensagemApi,
                    statusCode);
            }

            if (statusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                return CriarResultado(StatusPresenca.QrInvalido,
                    string.IsNullOrWhiteSpace(mensagemApi) ? "QR Code inválido" : mensagemApi,
                    statusCode);
            }

            if ((int)statusCode >= 500 && (int)statusCode <= 599)
            {
                return CriarResultado(StatusPresenca.ErroServidor,
                    "Não foi possível registrar a presença. Tente novamente em instantes.",
                    statusCode);
            }

            return CriarResultado(StatusPresenca.ErroServidor,
                "Não foi possível registrar a presença. Tente novamente em instantes.",
                statusCode);
        }
        catch (HttpRequestException)
        {
            return CriarFalhaDeConexao();
        }
        catch (TaskCanceledException)
        {
            return CriarFalhaDeConexao();
        }
    }

    private static ResultadoPresenca CriarResultado(StatusPresenca status, string mensagem, HttpStatusCode statusHttp) => new()
    {
        Status = status,
        Mensagem = mensagem,
        StatusHttp = statusHttp
    };

    private static ResultadoPresenca CriarFalhaDeConexao() => new()
    {
        Status = StatusPresenca.SemConexao,
        Mensagem = "Não foi possível conectar. Verifique sua internet e tente novamente.",
        StatusHttp = null
    };
}