using NotasMax.Models.Simulados;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using NotasMax.Views.Aluno;
using NotasMax.Views.Professor;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class StudentAndTeacherResultTests
{
    public StudentAndTeacherResultTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public void StudentSimulationDetailsDisplaysApiSummaryAndSubjects()
    {
        using var culture = new TestCultureScope("pt-BR");
        var service = new FakeSimuladoService
        {
            DesempenhoBySimulado = new DesempenhoAlunoBySimulado
            {
                Numero = 5,
                DataRealizacao = new DateTime(2026, 6, 10),
                Media = 8.2,
                Disciplinas =
                [
                    new DesempenhoAlunoBySimulado.Disciplina { Nome = "História", Nota = 7.4 },
                    new DesempenhoAlunoBySimulado.Disciplina { Nome = "Matemática", Nota = 9.0 }
                ]
            }
        };
        var view = new ExibirSimuladoView(service)
        {
            SimuladoSelecionado = new DesempenhoAlunoBySimulados.Simulado { Id = "sim-5", Numero = 5 }
        };

        view.InvokeMember("OnAppearing");

        Assert.Equal("Simulado 5", view.GetFieldValue<Label>("label_simulado").Text);
        Assert.Equal("8,20", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Equal(2, view.GetFieldValue<VerticalStackLayout>("layout_disciplinas").Children.Count);
        Assert.Equal("GetDesempenhoAlunoBySimulado", Assert.Single(service.Calls).Method);
    }

    [Fact]
    public async Task TeacherCanViewAStudentSimulationSummary()
    {
        using var culture = new TestCultureScope("pt-BR");
        var service = CreateDisciplineService();
        var view = CreateTeacherView(service);

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Equal("AM", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("Disciplina: História", view.GetFieldValue<Label>("label_contexto_disciplina").Text);
        Assert.Equal("4,47", view.GetFieldValue<Label>("label_valor_media").Text);
        var notes = GetNotes(view);
        Assert.Collection(notes,
            n => { Assert.Equal("1", n.Numero); Assert.Equal(1, n.Bimestre); Assert.Equal(4d, n.Nota); Assert.Equal("1ºB/S1", n.IdentificacaoGrafico); },
            n => { Assert.Equal("1", n.Numero); Assert.Equal(2, n.Bimestre); Assert.Equal(8d, n.Nota); Assert.Equal("2ºB/S1", n.IdentificacaoGrafico); });
        Assert.Equal(2, view.GetFieldValue<VerticalStackLayout>("layout_simulados").Children.Count);
        Assert.Equal(new[] { "sim-1/history", "sim-2/history", "missing/history" }, service.Calls.Where(c => c.Method == "GetGestaoNotasSimulado").Select(c => c.Id));
        Assert.Contains(service.Calls, c => c.Method == "GetByAluno" && c.Id == "student-5" && c.Token == "synthetic-token");
    }

    [Fact]
    public async Task TeacherPerformanceFallsBackToDatesWhenBimestreLookupFails()
    {
        var service = CreateDisciplineService();
        service.FailingMethods.Add("GetByAluno");
        var view = CreateTeacherView(service);
        await InvokeTask(view, "CarregarDados");
        var notes = GetNotes(view);
        Assert.Equal(2, notes.Count);
        Assert.All(notes, n => Assert.Null(n.Bimestre));
        Assert.Equal("S1 (01/03)", notes[0].IdentificacaoGrafico);
    }

    [Fact]
    public async Task TeacherPerformanceContinuesWhenOneSimulationFails()
    {
        var service = CreateDisciplineService();
        service.FailingMethods.Add("GetGestaoNotasSimulado:sim-1/history");
        var view = CreateTeacherView(service);
        await InvokeTask(view, "CarregarDados");
        Assert.Equal(8d, Assert.Single(GetNotes(view)).Nota);
    }

    [Fact]
    public async Task TeacherPerformanceHandlesMissingStudentAndEmptySimulations()
    {
        var service = new FakeSimuladoService();
        var view = CreateTeacherView(service);
        await InvokeTask(view, "CarregarDados");
        Assert.Equal("--", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Empty(GetNotes(view));
    }

    [Fact]
    public async Task TeacherPerformanceHandlesDisciplineRequestFailure()
    {
        var service = CreateDisciplineService();
        service.FailingMethods.Add("getDesempenhoByDisciplina");
        var view = CreateTeacherView(service);
        await InvokeTask(view, "CarregarDados");
        Assert.Equal("getDesempenhoByDisciplina", Assert.Single(service.Calls).Method);
        Assert.Empty(view.GetFieldValue<VerticalStackLayout>("layout_simulados").Children);
    }

    private static DesempenhoAlunoView CreateTeacherView(FakeSimuladoService service) =>
        new(service, new FakeSettingsService()) { AlunoId = "student-5", AlunoNome = "Ana Maria", DisciplinaId = "history", DisciplinaNome = "História" };

    private static List<NotaSimulado> GetNotes(DesempenhoAlunoView view) =>
        Assert.IsAssignableFrom<IEnumerable<NotaSimulado>>(view.GetFieldValue<object>("line_chart").GetType().GetProperty("ItemsSource")!.GetValue(view.GetFieldValue<object>("line_chart"))).ToList();

    private static FakeSimuladoService CreateDisciplineService()
    {
        var service = new FakeSimuladoService
        {
            DesempenhoByDisciplina = new DesempenhoAlunoByDisciplina
            {
                Alunos = [new() { Id = "other", Media = 10 }, new() { Id = "student-5", Media = 4.47 }],
                SimuladosContainer = new() { Simulados = [new() { SimuladoId = "sim-2", NumeroSimulado = 1 }, new() { SimuladoId = "sim-1", NumeroSimulado = 1 }, new() { SimuladoId = "sim-1", NumeroSimulado = 1 }, new() { SimuladoId = "outside", NumeroSimulado = 2 }, new() { SimuladoId = "missing", NumeroSimulado = 3 }, new() { SimuladoId = "", NumeroSimulado = 4 }] }
            },
            SimuladosAluno = new() { Simulados = [new() { Id = "sim-2", Media = 9, DataRealizacao = new DateTime(2026, 5, 1) }, new() { Id = "sim-1", Media = 7, DataRealizacao = new DateTime(2026, 3, 1) }, new() { Id = "sim-1", DataRealizacao = new DateTime(2026, 3, 1) }, new() { Id = "missing", DataRealizacao = new DateTime(2026, 6, 1) }, new() { Id = "" }] },
            ByAluno = new() { Simulados = [new() { Id = "sim-1", Bimestre = 1 }, new() { Id = "sim-2", Bimestre = 2 }, new() { Id = "sim-1", Bimestre = 1 }, new() { Id = "" }] }
        };
        service.GradesBySimulation["sim-1"] = new() { Alunos = [new() { Id = "other", Nota = 10 }, new() { Id = "student-5", Nota = 4 }] };
        service.GradesBySimulation["sim-2"] = new() { Alunos = [new() { Id = "student-5", Nota = 8 }] };
        return service;
    }

    [Fact]
    public void TeacherClassesPageDisplaysClassAndStudentCounts()
    {
        var simulations = new FakeSimuladoService
        {
            TurmasByProfessor = new TurmasByAnoAndProfessor
            {
                TotalTurmas = 4,
                TotalAlunos = 83,
                Turmas = [new TurmasByAnoAndProfessor.Turma { Serie = 1 }, new TurmasByAnoAndProfessor.Turma { Serie = 2 }]
            }
        };
        var view = new TurmasView(simulations, new FakeSettingsService(), new FakeNavigationService());

        view.InvokeMember("OnAppearing");

        Assert.Equal("4", view.GetFieldValue<Label>("label_numero_turmas_ativas").Text);
        Assert.Equal("83", view.GetFieldValue<Label>("label_numero_total_alunos").Text);
        Assert.Equal(2, view.GetFieldValue<VerticalStackLayout>("layout_turmas").Children.Count);
    }

    [Fact]
    public void SimulationDetailsReturnWithoutFetchingWhenNoSimulationWasSelected()
    {
        var service = new FakeSimuladoService();
        var view = new ExibirSimuladoView(service);

        view.InvokeMember("OnAppearing");

        Assert.Empty(service.Calls);
    }

    [Fact]
    public async Task TeacherStudentPerformanceReturnsForAnEmptySimulationList()
    {
        var service = new FakeSimuladoService { SimuladosAluno = new DesempenhoAlunoBySimulados() };
        var view = new DesempenhoAlunoView(service) { AlunoId = "student-empty", AlunoNome = null! };

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Aluno", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Equal("AA", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("--", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Empty(service.Calls);
        Assert.Empty(GetNotes(view));
    }

    [Fact]
    public void TeacherClassesPageReturnsWhenTheServiceFails()
    {
        var service = new FakeSimuladoService { ExceptionToThrow = new InvalidOperationException("synthetic failure") };
        var view = new TurmasView(service, new FakeSettingsService(), new FakeNavigationService());

        view.InvokeMember("OnAppearing");

        Assert.Equal("0", view.GetFieldValue<Label>("label_numero_turmas_ativas").Text);
    }

    private static Task InvokeTask(object view, string name, params object?[] arguments) =>
        (Task)view.InvokeMember(name, arguments)!;
}
