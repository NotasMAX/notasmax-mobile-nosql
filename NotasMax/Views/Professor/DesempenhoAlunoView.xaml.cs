using NotasMax.Helpers;
using NotasMax.Services.Simulados;
using NotasMax.Services.Settings;
using NotasMax.ViewModels;
using System.Diagnostics;

namespace NotasMax.Views.Professor;

[QueryProperty(nameof(AlunoId), "AlunoId")]
[QueryProperty(nameof(AlunoNome), "AlunoNome")]
[QueryProperty(nameof(DisciplinaId), "DisciplinaId")]
[QueryProperty(nameof(DisciplinaNome), "DisciplinaNome")]
public partial class DesempenhoAlunoView : ContentPage
{
    private readonly ISimuladoService _simuladoService;
    private readonly ISettingsService? _settingsService;

    public string AlunoId { get; set; }
    public string AlunoNome { get; set; }
    public string DisciplinaId { get; set; } = string.Empty;
    public string DisciplinaNome { get; set; } = string.Empty;

    public DesempenhoAlunoView(ISimuladoService simuladoService, ISettingsService? settingsService = null)
    {
        InitializeComponent();
        _simuladoService = simuladoService;
        _settingsService = settingsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDados();
    }

    private async Task CarregarDados()
    {
        string nome = AlunoNome ?? "Aluno";
        label_nome_aluno.Text = nome;
        label_iniciais.Text = TextoHelper.PegarIniciais(nome);
        label_contexto_disciplina.Text = string.IsNullOrWhiteSpace(DisciplinaNome)
            ? "Média da disciplina" : $"Disciplina: {DisciplinaNome}";

        try
        {
            // Apenas a disciplina selecionada pelo professor na tela anterior.
            if (string.IsNullOrWhiteSpace(DisciplinaId))
            {
                label_valor_media.Text = "--";
                line_chart.ItemsSource = new List<NotaSimulado>();
                BindableLayout.SetItemsSource(layout_simulados, new List<NotaSimulado>());
                return;
            }

            var desempenho = await _simuladoService.getDesempenhoByDisciplina(DisciplinaId);
            var aluno = desempenho?.Alunos.FirstOrDefault(a => a.Id == AlunoId);
            label_valor_media.Text = aluno == null ? "--" : aluno.Media.ToString("N2");

            // O número é único apenas dentro do bimestre. O ID identifica a avaliação.
            var resultado = await _simuladoService.GetSimuladosByAluno(AlunoId);
            var datas = resultado?.Simulados?
                .Where(s => !string.IsNullOrWhiteSpace(s.Id))
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First().DataRealizacao)
                ?? new Dictionary<string, DateTime>();
            var bimestres = new Dictionary<string, int>();
            try
            {
                var detalhes = await _simuladoService.GetByAluno(AlunoId, _settingsService?.AuthAccessToken ?? string.Empty);
                bimestres = detalhes?.Simulados?
                    .Where(s => !string.IsNullOrWhiteSpace(s.Id))
                    .GroupBy(s => s.Id)
                    .ToDictionary(g => g.Key, g => g.First().Bimestre)
                    ?? new Dictionary<string, int>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao consultar bimestres: {ex.Message}");
            }

            var notas = new List<NotaSimulado>();
            var simuladosDisciplina = desempenho?.SimuladosContainer?.Simulados
                .Where(s => !string.IsNullOrWhiteSpace(s.SimuladoId) && datas.ContainsKey(s.SimuladoId))
                .GroupBy(s => s.SimuladoId)
                .Select(g => g.First())
                .OrderBy(s => bimestres.TryGetValue(s.SimuladoId, out var bi) && bi > 0 ? bi : int.MaxValue)
                .ThenBy(s => datas[s.SimuladoId])
                .ThenBy(s => s.NumeroSimulado)
                .ToList();

            foreach (var simulado in simuladosDisciplina ?? new())
            {
                try
                {
                    var detalhe = await _simuladoService.GetGestaoNotasSimulado(simulado.SimuladoId, DisciplinaId);
                    var notaAluno = detalhe?.Alunos.FirstOrDefault(a => a.Id == AlunoId);
                    if (notaAluno == null) continue;

                    notas.Add(new NotaSimulado
                    {
                        Numero = simulado.NumeroSimulado.ToString(),
                        Bimestre = bimestres.TryGetValue(simulado.SimuladoId, out var bimestre) ? bimestre : null,
                        Nota = notaAluno.Nota,
                        Data = datas[simulado.SimuladoId]
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro ao carregar nota do simulado {simulado.SimuladoId}: {ex.Message}");
                }
            }

            line_chart.ItemsSource = notas;
            BindableLayout.SetItemsSource(layout_simulados, notas);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar desempenho do aluno: {ex.Message}");
        }
    }
}