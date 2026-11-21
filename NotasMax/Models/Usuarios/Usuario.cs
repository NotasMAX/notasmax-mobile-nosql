using System.Text.Json.Serialization;

namespace NotasMax.Models.Usuarios
{
    public class Usuario
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("nome")]
        public string? Nome { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("tipo_usuario")]
        public string? TipoUsuario { get; set; }

        [JsonPropertyName("usa_senha_inicial")]
        public bool UsaSenhaInicial { get; set; }

        [JsonPropertyName("telefone_responsavel")]
        public string? TelefoneResponsavel { get; set; }
        
        [JsonPropertyName("nome_responsavel")]
        public string? NomeResponsavel { get; set; }

    }
}
