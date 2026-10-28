using NotasMax.Models.Simulados;
using NotasMax.Models.Usuarios;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using NotasMax.Views.Aluno;
using NotasMax.Views.Professor;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class PageLoadingTests
{
    public PageLoadingTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public void StudentPersonalDetailsReadValuesFromSettings()
    {
        var settings = new FakeSettingsService
        {
            UserNameKey = "Ana Maria",
            UserEmailKey = "ana@example.invalid",
            UserNomeResponsavelKey = "Responsável",
            UserTelefoneResponsavelKey = "11999990000"
        };
        var view = new DadosPessoaisAlunoView(settings);

        view.InvokeMember("OnAppearing");

        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_nome").Text);
        Assert.Equal("AM", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("ana@example.invalid", view.GetFieldValue<Label>("label_email").Text);
        Assert.Equal("Responsável", view.GetFieldValue<Label>("label_nome_responsavel").Text);
        Assert.Equal("11999990000", view.GetFieldValue<Label>("label_telefone_responsavel").Text);
    }

    [Fact]
    // Baseline only: this simple average is not an approved academic calculation rule.
    [Trait("Category", "Baseline non-normative")]
    public async Task StudentProfileDisplaysCurrentSummaryAndEmptyState()
    {
        var service = new FakeSimuladoService
        {
            DesempenhoAluno = new DesempenhoAluno
            {
                Simulados = [new DesempenhoAluno.Simulado { Media = 6 }, new DesempenhoAluno.Simulado { Media = 8 }],
                Materias = [new DesempenhoAluno.Materia { Nome = "Matemática" }]
            }
        };
        var view = new PerfilView(new FakeSettingsService { UserNameKey = "Ana Maria" }, new FakeNavigationService(), new FakeUsuarioService(), service);

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_nome").Text);
        Assert.Equal("AM", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("7,0", view.GetFieldValue<Label>("label_nota_geral").Text);
        Assert.Equal("2", view.GetFieldValue<Label>("label_qtd_simulados").Text);
        Assert.Equal("1", view.GetFieldValue<Label>("label_qtd_materias").Text);

        service.DesempenhoAluno = new DesempenhoAluno();
        await InvokeTask(view, "CarregarDados");

        Assert.Equal("--", view.GetFieldValue<Label>("label_nota_geral").Text);
        Assert.Equal("0", view.GetFieldValue<Label>("label_qtd_simulados").Text);
    }

    [Fact]
    public async Task StudentProfileExitRequestsLoginNavigation()
    {
        var navigation = new FakeNavigationService();
        var view = new PerfilView(new FakeSettingsService(), navigation, new FakeUsuarioService(), new FakeSimuladoService());

        view.InvokeMember("Sair_Tapped", null!, new TappedEventArgs(null));
        await Task.CompletedTask;

        Assert.Equal(("Login?Logout=true", true), Assert.Single(navigation.Navigations));
    }

    [Fact]
    public async Task ProfessorHomeDisplaysSummaryAndFiltersChartBySelectedYear()
    {
        var year = DateTime.UtcNow.Year;
        var users = new FakeUsuarioService { User = new Usuario { Id = "teacher-1", Nome = "Carlos Silva" } };
        var simulations = new FakeSimuladoService
        {
            ByProfessor = new SimuladoProfessorResponse
            {
                TotalTurmas = 3,
                TotalSimuladosAplicados = 11,
                MediaGeralTodasTurmas = 7.2,
                Turmas =
                [
                    new TurmaResumo { TurmaId = "a", Ano = year, Serie = 2, MediaGeralTurma = 8 },
                    new TurmaResumo { TurmaId = "b", Ano = year - 1, Serie = 1, MediaGeralTurma = 6 },
                    new TurmaResumo { TurmaId = "c", Ano = year, Serie = 1, MediaGeralTurma = 7 }
                ]
            }
        };
        var view = new HomeProfessorView(new FakeNavigationService(), new FakeSettingsService(), users, simulations);

        view.InvokeMember("OnAppearing");

        Assert.Equal("Carlos Silva", view.GetFieldValue<Label>("txt_nomeUsuario").Text);
        Assert.Equal("3", view.GetFieldValue<Label>("txt_totalTurmas").Text);
        Assert.Equal("11", view.GetFieldValue<Label>("txt_totalSimulados").Text);
        Assert.Equal("7,2", view.GetFieldValue<Label>("txt_notaGeral").Text);

        var picker = view.GetFieldValue<Picker>("picker_ano");
        Assert.Equal(new[] { year.ToString(), (year - 1).ToString() }, picker.ItemsSource);
        picker.SelectedIndex = 1;
        view.InvokeMember("Picker_Ano_Changed", picker, EventArgs.Empty);

        var chart = view.GetFieldValue<object>("serie_turmas");
        var chartItems = chart.GetType().GetProperty("ItemsSource")!.GetValue(chart);
        Assert.Collection(Assert.IsAssignableFrom<IEnumerable<TurmaResumo>>(chartItems), item => Assert.Equal(year - 1, item.Ano));
    }

    [Fact]
    public async Task ProfessorHomeShowsEmptyChartForNoClassesAndIgnoresMissingYearSelection()
    {
        var view = new HomeProfessorView(
            new FakeNavigationService(),
            new FakeSettingsService(),
            new FakeUsuarioService(),
            new FakeSimuladoService { ByProfessor = new NotasMax.Models.Simulados.SimuladoProfessorResponse() });

        await InvokeTask(view, "CarregarDados", "teacher-empty");
        var picker = view.GetFieldValue<Picker>("picker_ano");
        view.InvokeMember("Picker_Ano_Changed", picker, EventArgs.Empty);
        view.InvokeMember("AtualizarGrafico", DateTime.UtcNow.Year);

        Assert.Empty((IEnumerable<string>)picker.ItemsSource);
        Assert.False(view.GetFieldValue<VisualElement>("grafico_turmas").IsVisible);
        Assert.True(view.GetFieldValue<VisualElement>("lbl_grafico_vazio").IsVisible);
    }

    [Fact]
    public async Task ProfessorProfileDeduplicatesSubjectsAndShowsClassSummary()
    {
        var settings = new FakeSettingsService { UserIdKey = "teacher-1", UserNameKey = "Carlos Silva" };
        var users = new FakeUsuarioService { User = new Usuario { Email = "carlos@example.invalid" } };
        var simulations = new FakeSimuladoService
        {
            TurmasByProfessor = new TurmasByAnoAndProfessor
            {
                TotalTurmas = 2,
                TotalAlunos = 36,
                Turmas =
                [
                    new TurmasByAnoAndProfessor.Turma
                    {
                        Serie = 2,
                        Disciplinas = [new TurmasByAnoAndProfessor.Disciplina { MateriaId = "math", MateriaNome = "Matemática" }]
                    },
                    new TurmasByAnoAndProfessor.Turma
                    {
                        Serie = 3,
                        Disciplinas =
                        [
                            new TurmasByAnoAndProfessor.Disciplina { MateriaId = "math", MateriaNome = "Matemática" },
                            new TurmasByAnoAndProfessor.Disciplina { MateriaId = "history", MateriaNome = "História" }
                        ]
                    }
                ]
            }
        };
        var view = new PerfilProfessorView(settings, new FakeNavigationService(), users, simulations);

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Carlos Silva", view.GetFieldValue<Label>("label_nome").Text);
        Assert.Equal("CS", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("carlos@example.invalid", view.GetFieldValue<Label>("label_email").Text);
        Assert.Equal("2", view.GetFieldValue<Label>("label_qtd_turmas").Text);
        Assert.Equal("36", view.GetFieldValue<Label>("label_qtd_alunos").Text);
        Assert.Equal("2", view.GetFieldValue<Label>("label_qtd_materias").Text);
        Assert.Equal(2, view.GetFieldValue<FlexLayout>("layout_chips_materias").Children.Count);
        Assert.Equal(2, view.GetFieldValue<FlexLayout>("layout_chips_turmas").Children.Count);

        view.InvokeMember("ToggleMaterias_Tapped", null!, null!);
        view.InvokeMember("ToggleTurmas_Tapped", null!, null!);
        Assert.False(view.GetFieldValue<FlexLayout>("layout_chips_materias").IsVisible);
        Assert.False(view.GetFieldValue<FlexLayout>("layout_chips_turmas").IsVisible);
    }

    private static Task InvokeTask(object view, string name, params object?[] arguments) =>
        (Task)view.InvokeMember(name, arguments)!;
}
