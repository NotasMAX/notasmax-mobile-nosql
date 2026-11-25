using NotasMax.Helpers;
using NotasMax.Services.Simulados;
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

    public string AlunoId { get; set; }
    public string AlunoNome { get; set; }
    public string DisciplinaId { get; set; } = string.Empty;
    public string DisciplinaNome { get; set; } = string.Empty;

    public DesempenhoAlunoView(ISimuladoService simuladoService)
    {
        InitializeComponent();
        _simuladoService = simuladoService;
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

            // A consulta geral fornece datas; a consulta por disciplina delimita
            // os simulados e a consulta de notas fornece a nota deste aluno.
            var resultado = await _simuladoService.GetSimuladosByAluno(AlunoId);
            var datas = resultado?.Simulados?.ToDictionary(s => s.Id, s => s.DataRealizacao)
                        ?? new Dictionary<string, DateTime>();
            var notas = new List<NotaSimulado>();
            var simuladosDisciplina = desempenho?.SimuladosContainer?.Simulados
                .Where(s => !string.IsNullOrWhiteSpace(s.SimuladoId))
                .GroupBy(s => s.SimuladoId)
                .Select(g => g.First())
                .OrderBy(s => s.NumeroSimulado)
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
                        Nota = notaAluno.Nota,
                        Data = datas.TryGetValue(simulado.SimuladoId, out var data) ? data : default
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