using NotasMax.Helpers;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.ViewModels;
using Syncfusion.Maui.Toolkit.Charts;
using System.Diagnostics;

namespace NotasMax.Views.Professor;

[QueryProperty(nameof(TurmaId), "turmaId")]
public partial class DesempenhoTurmaView : ContentPage
{
    public string? TurmaId { get; set; }
    public TurmasByAnoAndProfessor.Turma? TurmaSelecionada { get; set; }

    private List<Brush> PaleteBrushes = new();
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly ISimuladoService _simuladoService;
    private bool isTurmaLoading = false;
    private bool isDisciplinaLoading = false;

    public DesempenhoTurmaView(ISettingsService settingsService, INavigationService navigationService, ISimuladoService simuladoService )
    {
        InitializeComponent();
        _settingsService = settingsService;
        _navigationService = navigationService;
        _simuladoService = simuladoService;
    }

    private void CalcularMenorNota(List<DesempenhoAlunoByDisciplina.DesempenhoAluno> alunos)
    {
        var menorNota = alunos.MinBy(n => n.Media);
        if (menorNota != null)
        {
            label_menor_nota.Text = menorNota.Media.ToString("N2");
            label_menor_aluno.Text = TextoHelper.ReduzirNome(menorNota.Nome);
        }
    }

    private void CalcularMaiorNota(List<DesempenhoAlunoByDisciplina.DesempenhoAluno> alunos)
    {
        var maiorNota = alunos.MaxBy(n => n.Media);
        if (maiorNota == null)
            return;

        label_maior_nota.Text = maiorNota.Media.ToString("N2");
        label_maior_aluno.Text = TextoHelper.ReduzirNome(maiorNota.Nome);

    }
    private void DefinirDestaqueDistribuicao(List<DesempenhoAlunoByDisciplina.DesempenhoDistribuicao> distribuicaoNotas)
    {
        int indexNotasBaixas = distribuicaoNotas.FindIndex(item => item.Faixa == "<5");
        doughnut_chart.ExplodeIndex = indexNotasBaixas;
    }

    private async Task<TurmasByAnoAndProfessor?> fetchDadosTurmas()
    {
        try
        {
            return await _simuladoService.GetByAnoAndProfessor();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro: {ex.Message}");
            return null;
        }
    }


    private async Task<DesempenhoAlunoByDisciplina?> fetchDadosDisciplinas(string disciplinaId)
    {
        try
        {
            return await _simuladoService.getDesempenhoByDisciplina(disciplinaId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro: {ex.Message}");
            return null;
        }
    }


    protected async override void OnAppearing()
    {
        base.OnAppearing();

        TurmasByAnoAndProfessor? dadosTurmas = await fetchDadosTurmas();

        if (dadosTurmas == null)
            return;

        dadosTurmas.Turmas = dadosTurmas.Turmas.OrderBy(t => t.Serie).ToList();

        chirp_serie.ItemsSource = dadosTurmas.Turmas;

        if (!string.IsNullOrEmpty(TurmaId))
        {
            chirp_serie.SelectedItem = dadosTurmas.Turmas.FirstOrDefault(t => t.Id == TurmaId);
        }
        else
        {
            chirp_serie.SelectedItem = dadosTurmas.Turmas.MinBy(t => t.Serie);
        }
    }
    private void DefinirCoresDistribuicao(List<DesempenhoAlunoByDisciplina.DesempenhoDistribuicao> distribuicaoNotas)
    {
        DesempenhoAlunoByDisciplina.DesempenhoDistribuicao primeiraFaixa = new();
        DesempenhoAlunoByDisciplina.DesempenhoDistribuicao segundaFaixa = new();
        DesempenhoAlunoByDisciplina.DesempenhoDistribuicao terceiraFaixa = new();
        List<DesempenhoAlunoByDisciplina.DesempenhoDistribuicao> notasDistribuidas = new();

        foreach (var item in distribuicaoNotas)
        {
            switch (item.Faixa)
            {
                case "<5":
                    primeiraFaixa = item;
                    break;

                case "5-7":
                    segundaFaixa = item;
                    break;

                case ">7":
                    terceiraFaixa = item;
                    break;
            }
        };

        // Definir as cores para cada faixa usando o ColorHelper
        // Faixa <5: Vermelho, Faixa 5-7: Amarelo, Faixa >7: Verde
        Color vermelho = ColorHelper.DefinirCor(0);
        Color amarelo = ColorHelper.DefinirCor(6);
        Color verde = ColorHelper.DefinirCor(10);

        notasDistribuidas.Add(primeiraFaixa);

        PaleteBrushes.Add(new SolidColorBrush(vermelho));
        notasDistribuidas.Add(segundaFaixa);
        PaleteBrushes.Add(new SolidColorBrush(amarelo));
        notasDistribuidas.Add(terceiraFaixa);
        PaleteBrushes.Add(new SolidColorBrush(verde));

        doughnut_chart.ItemsSource = notasDistribuidas;
        doughnut_chart.PaletteBrushes = PaleteBrushes;

        DefinirDestaqueDistribuicao(notasDistribuidas);
    }



    private void ColumnChart_LabelCreated(object sender, ChartAxisLabelEventArgs e)
    {
        string? labelText = e.Label;

        if (labelText == null)
            return;

        e.Label = TextoHelper.ReduzirNome(labelText);
    }

    private async void chirp_serie_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        
        if (isTurmaLoading)
            return;

        isTurmaLoading = true;

        if (!(e.AddedItem is TurmasByAnoAndProfessor.Turma turma))
            return;

        List<TurmasByAnoAndProfessor.Disciplina> disciplinas = turma.Disciplinas.OrderBy(d => d.MateriaNome).ToList();

        chirp_disciplinas.ItemsSource = disciplinas;
        var primeiraDisciplina = disciplinas.FirstOrDefault();
        chirp_disciplinas.SelectedItem = primeiraDisciplina;

        if (primeiraDisciplina != null)
        {
            await CarregarDadosDisciplinaAsync(primeiraDisciplina);
        }

        isTurmaLoading = false;
    }

    private async void chirp_disciplinas_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        if (!(e.AddedItem is TurmasByAnoAndProfessor.Disciplina disciplina))
            return;
        await CarregarDadosDisciplinaAsync(disciplina);
    }

    private async Task CarregarDadosDisciplinaAsync(TurmasByAnoAndProfessor.Disciplina disciplina)
    {
        if (isDisciplinaLoading)
            return;

        isDisciplinaLoading = true;

        Debug.WriteLine($"Disciplina selecionada: {disciplina.MateriaNome} (ID: {disciplina.Id})");

        var dadosDisciplinas = await fetchDadosDisciplinas(disciplina.Id);

        if (dadosDisciplinas == null)
            return;

        var alunos = dadosDisciplinas.Alunos.OrderBy(a => a.Nome).ToList();
        foreach (var aluno in alunos)
        {
            aluno.DisciplinaId = disciplina.Id;
            aluno.DisciplinaNome = disciplina.MateriaNome;
        }
        var simulados = dadosDisciplinas.SimuladosContainer.Simulados.OrderBy(s => s.NumeroSimulado).ToList();
        var distribuicaoNotas = dadosDisciplinas.Distribuicao;
        var media = dadosDisciplinas.SimuladosContainer.Media;
        DefinirCoresDistribuicao(distribuicaoNotas);

        line_chart.ItemsSource = simulados;
        column_chart.ItemsSource = alunos;

        CalcularMenorNota(alunos);
        CalcularMaiorNota(alunos);

        label_media.Text = media.ToString("N2");


        label_subtitulo_alunos.Text = $"Alunos ({alunos.Count})";

        BindableLayout.SetItemsSource(layout_alunos, alunos);

        isDisciplinaLoading = false;
    }

    private void chirp_simulados_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }
}