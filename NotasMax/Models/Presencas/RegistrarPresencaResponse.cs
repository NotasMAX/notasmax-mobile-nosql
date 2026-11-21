using System.Text.Json.Serialization;

namespace NotasMax.Models.Presencas;

public class RegistrarPresencaResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("simulado")]
    public SimuladoPresencaResponse? Simulado { get; set; }
}

public class SimuladoPresencaResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("numero")]
    public int Numero { get; set; }

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("data_realizacao")]
    public DateTime DataRealizacao { get; set; }
}