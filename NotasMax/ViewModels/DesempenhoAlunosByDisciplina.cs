using System.Text.Json.Serialization;

namespace NotasMax.ViewModels
{
    public class DesempenhoAlunoByDisciplina
    {
        [JsonPropertyName("alunos")]
        public List<DesempenhoAluno> Alunos { get; set; } = new();
        [JsonPropertyName("simulados")]
        public SimuladoContainer SimuladosContainer { get; set; } = new();
        [JsonPropertyName("distribuicao")]
        public List<DesempenhoDistribuicao> Distribuicao { get; set; } = new();

        public class SimuladoContainer
        {
            [JsonPropertyName("media")]
            public double Media { get; set; }

            [JsonPropertyName("notas")]
            public List<DesempenhoSimulado> Simulados { get; set; } = new();
        }

        public class DesempenhoAluno
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("nome")]
            public string Nome { get; set; }
            [JsonPropertyName("media")]
            public double Media { get; set; }
            [JsonIgnore]
            public string DisciplinaId { get; set; } = string.Empty;
            [JsonIgnore]
            public string DisciplinaNome { get; set; } = string.Empty;
        }

        public class DesempenhoSimulado
        {
            [JsonPropertyName("simulado_id")]
            public string SimuladoId { get; set; }
            [JsonPropertyName("numero_simulado")]
            public int NumeroSimulado { get; set; }
            [JsonPropertyName("media")]
            public double Media { get; set; }
        }

        public class DesempenhoDistribuicao
        {
            [JsonPropertyName("faixa")]
            public string Faixa { get; set; }
            [JsonPropertyName("quantidade")]
            public int Quantidade { get; set; }
        }
    }
}
