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
    // Baseline only: the displayed simple average is not an approved academic calculation rule.
    [Trait("Category", "Baseline non-normative")]
    public async Task TeacherCanViewAStudentSimulationSummary()
    {
        using var culture = new TestCultureScope("pt-BR");
        var simulations = new FakeSimuladoService
        {
            SimuladosAluno = new DesempenhoAlunoBySimulados
            {
                Simulados =
                [
                    new DesempenhoAlunoBySimulados.Simulado { Id = "sim-2", Numero = 2, Media = 9, DataRealizacao = new DateTime(2026, 5, 1) },
                    new DesempenhoAlunoBySimulados.Simulado { Id = "sim-1", Numero = 1, Media = 7, DataRealizacao = new DateTime(2026, 3, 1) }
                ]
            }
        };
        var view = new DesempenhoAlunoView(simulations) { AlunoId = "student-5", AlunoNome = "Ana Maria" };

        await InvokeTask(view, "CarregarDados");

        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Equal("AM", view.GetFieldValue<Label>("label_iniciais").Text);
        Assert.Equal("8,00", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Equal("GetSimuladosByAluno", Assert.Single(simulations.Calls).Method);
        Assert.Equal("student-5", simulations.Calls[0].Id);
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
        Assert.Equal("0.00", view.GetFieldValue<Label>("label_valor_media").Text);
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
