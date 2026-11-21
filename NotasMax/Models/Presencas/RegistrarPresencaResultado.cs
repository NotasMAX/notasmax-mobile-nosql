using System.Net;

namespace NotasMax.Models.Presencas;

public enum StatusPresenca
{
    Sucesso,
    JaRegistrada,
    TurmaDiferente,
    QrInvalido,
    SessaoExpirada,
    SemConexao,
    ErroServidor
}

public class ResultadoPresenca
{
    public StatusPresenca Status { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public HttpStatusCode? StatusHttp { get; set; }
    public SimuladoPresencaResponse? Simulado { get; set; }
}