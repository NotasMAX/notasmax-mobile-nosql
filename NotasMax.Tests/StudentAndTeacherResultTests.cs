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
    public async Task TeacherSeesOnlySelectedDisciplineGradesAndApiAverage()
    {
        using var culture = new TestCultureScope("pt-BR");
        var date = new DateTime(2026, 3, 1);
        var service = new FakeSimuladoService
        {
            DesempenhoByDisciplina = new DesempenhoAlunoByDisciplina
            {
                Alunos = [new() { Id = "other", Media = 9 }, new() { Id = "student-5", Media = 4.47 }],
                SimuladosContainer = new()
                {
                    Simulados = [new() { SimuladoId = "sim-2", NumeroSimulado = 2 },
                        new() { SimuladoId = "sim-1", NumeroSimulado = 1 },
                        new() { SimuladoId = "sim-1", NumeroSimulado = 1 },
                        new() { SimuladoId = "", NumeroSimulado = 3 },
                        new() { SimuladoId = "missing", NumeroSimulado = 4 },
                        new() { SimuladoId = "failure", NumeroSimulado = 5 }]
                }
            },
            SimuladosAluno = new()
            {
                Simulados = [new() { Id = "sim-1", Media = 9, DataRealizacao = date },
                    new() { Id = "unrelated", Media = 10 }]
            }
        };
        service.GradesBySimulation["sim-1/history"] = new() { Alunos = [new() { Id = "other", Nota = 10 }, new() { Id = "student-5", Nota = 4 }] };
        service.GradesBySimulation["sim-2/history"] = new() { Alunos = [new() { Id = "student-5", Nota = 5 }] };
        service.FailingGradeQueries.Add("failure/history");
        var view = new DesempenhoAlunoView(service)
        {
            AlunoId = "student-5", AlunoNome = "Ana Maria", DisciplinaId = "history", DisciplinaNome = "História"
        };

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Equal("AM", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("Disciplina: História", view.GetFieldValue<Label>("label_contexto_disciplina").Text);
        Assert.Equal("4,47", view.GetFieldValue<Label>("label_valor_media").Text);
        var notes = Assert.IsAssignableFrom<IEnumerable<NotaSimulado>>(BindableLayout.GetItemsSource(view.GetFieldValue<VerticalStackLayout>("layout_simulados")));
        Assert.Collection(notes,
            note => { Assert.Equal("1", note.Numero); Assert.Equal(4d, note.Nota); Assert.Equal(date, note.Data); },
            note => { Assert.Equal("2", note.Numero); Assert.Equal(5d, note.Nota); Assert.Equal(default(DateTime), note.Data); });
        Assert.Same(notes, view.GetFieldValue<Syncfusion.Maui.Toolkit.Charts.SplineSeries>("line_chart").ItemsSource);
        Assert.Equal("history", service.Calls[0].Id);
        Assert.Equal("student-5", service.Calls[1].Id);
        Assert.Equal(new[] { "sim-1/history", "sim-2/history", "missing/history", "failure/history" },
            service.Calls.Where(c => c.Method == "GetGestaoNotasSimulado").Select(c => c.Id));
    }

    [Fact]
    public async Task TeacherStudentPerformanceWithNoMatchingStudentShowsNoAverage()
    {
        var service = new FakeSimuladoService();
        var view = new DesempenhoAlunoView(service) { AlunoId = "missing", DisciplinaId = "history" };
        await InvokeTask(view, "CarregarDados");
        Assert.Equal("--", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<NotaSimulado>>(BindableLayout.GetItemsSource(view.GetFieldValue<VerticalStackLayout>("layout_simulados"))));
    }

    [Fact]
    public async Task TeacherStudentPerformanceHandlesUnavailableService()
    {
        var service = new FakeSimuladoService { ExceptionToThrow = new InvalidOperationException("synthetic failure") };
        var view = new DesempenhoAlunoView(service) { AlunoId = "student", AlunoNome = "Ana Maria", DisciplinaId = "history" };
        await InvokeTask(view, "CarregarDados");
        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Empty(service.Calls);
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
    public async Task TeacherStudentPerformanceDoesNotFetchWithoutDiscipline()
    {
        var service = new FakeSimuladoService { SimuladosAluno = new DesempenhoAlunoBySimulados() };
        var view = new DesempenhoAlunoView(service) { AlunoId = "student-empty", AlunoNome = null! };

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Aluno", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Equal("AA", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("--", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Empty(service.Calls);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<NotaSimulado>>(BindableLayout.GetItemsSource(view.GetFieldValue<VerticalStackLayout>("layout_simulados"))));
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
