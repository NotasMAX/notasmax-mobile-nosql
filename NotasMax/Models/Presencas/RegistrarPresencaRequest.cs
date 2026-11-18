using System.Text.Json.Serialization;

namespace NotasMax.Models.Presencas;

public class RegistrarPresencaRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}