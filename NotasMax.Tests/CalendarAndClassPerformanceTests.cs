using System.Reflection;
using System.Runtime.CompilerServices;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using NotasMax.Views.Aluno;
using NotasMax.Views.Professor;
using Syncfusion.Maui.Toolkit.Calendar;
using Syncfusion.Maui.Toolkit.Charts;
using Syncfusion.Maui.Toolkit.Chips;
using Xunit;
using ChipsSelectionChangedEventArgs = Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class CalendarAndClassPerformanceTests
{
    public CalendarAndClassPerformanceTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public async Task CalendarLoadsCurrentMonthAndMarksScheduledSimulations()
    {
        var today = DateTime.Today;
        var objectiveDay = new DateTime(today.Year, today.Month, Math.Min(8, DateTime.DaysInMonth(today.Year, today.Month)));
        var essayDay = new DateTime(today.Year, today.Month, Math.Min(18, DateTime.DaysInMonth(today.Year, today.Month)));
        var service = new FakeSimuladoService
        {
            Calendario = new CalendarioByAluno
            {
                Simulados =
                [
                    new CalendarioByAluno.CalendarioSimulado { Numero = 1, Tipo = "objetivo", DataRealizacao = objectiveDay },
                    new CalendarioByAluno.CalendarioSimulado { Numero = 2, Tipo = "discursivo", DataRealizacao = essayDay },
                    new CalendarioByAluno.CalendarioSimulado { Numero = 3, Tipo = "objetivo", DataRealizacao = today.AddMonths(-1) }
                ]
            }
        };
        var view = new CalendarioAlunoView(service);

        await InvokeTask(view, "InicializarCalendario");

        var calendar = view.GetFieldValue<SfCalendar>("calendar");
        Assert.Equal(2, view.GetFieldValue<VerticalStackLayout>("layout_eventos").Children.Count);

        var specialDays = calendar.MonthView.GetType().GetProperty("SpecialDayPredicate")!.GetValue(calendar.MonthView) as Delegate;
        Assert.NotNull(specialDays);
        Assert.IsType<CalendarIconDetails>(specialDays!.DynamicInvoke(objectiveDay));
        Assert.IsType<CalendarIconDetails>(specialDays.DynamicInvoke(essayDay));
        Assert.Null(specialDays.DynamicInvoke(today.AddDays(-2)));
        Assert.Single(service.Calls);
        Assert.Equal("GetCalendarioByAluno", service.Calls[0].Method);

        var viewChanged = (CalendarViewChangedEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(CalendarViewChangedEventArgs));
        var visibleDatesField = typeof(CalendarViewChangedEventArgs).GetField("<NewVisibleDates>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(visibleDatesField);
        visibleDatesField!.SetValue(viewChanged, new System.Collections.ObjectModel.ReadOnlyCollection<DateTime>(new List<DateTime> { today, today.AddMonths(-1), today }));
        view.InvokeMember("calendar_ViewChanged", calendar, viewChanged);
        Assert.Single(view.GetFieldValue<VerticalStackLayout>("layout_eventos").Children);
    }

    [Fact]
    public async Task CalendarSurvivesAnUnavailableCalendarResponse()
    {
        var service = new FakeSimuladoService { ExceptionToThrow = new InvalidOperationException("synthetic failure") };
        var view = new CalendarioAlunoView(service);

        await InvokeTask(view, "InicializarCalendario");

        Assert.Empty(view.GetFieldValue<VerticalStackLayout>("layout_eventos").Children);
    }

    [Fact]
    public async Task ClassPerformanceLoadsDistributionAndOrderedStudentResults()
    {
        var response = new DesempenhoAlunoByDisciplina
        {
            SimuladosContainer = new DesempenhoAlunoByDisciplina.SimuladoContainer
            {
                Media = 7.25,
                Simulados =
                [
                    new DesempenhoAlunoByDisciplina.DesempenhoSimulado { NumeroSimulado = 2, Media = 8 },
                    new DesempenhoAlunoByDisciplina.DesempenhoSimulado { NumeroSimulado = 1, Media = 6 }
                ]
            },
            Alunos =
            [
                new DesempenhoAlunoByDisciplina.DesempenhoAluno { Id = "b", Nome = "Bruno Silva", Media = 4 },
                new DesempenhoAlunoByDisciplina.DesempenhoAluno { Id = "a", Nome = "Ana Maria", Media = 9 }
            ],
            Distribuicao =
            [
                new DesempenhoAlunoByDisciplina.DesempenhoDistribuicao { Faixa = ">7", Quantidade = 1 },
                new DesempenhoAlunoByDisciplina.DesempenhoDistribuicao { Faixa = "<5", Quantidade = 1 },
                new DesempenhoAlunoByDisciplina.DesempenhoDistribuicao { Faixa = "5-7", Quantidade = 0 }
            ]
        };
        var service = new FakeSimuladoService { DesempenhoByDisciplina = response };
        var view = new DesempenhoTurmaView(new FakeSettingsService(), new FakeNavigationService(), service);
        var subject = new TurmasByAnoAndProfessor.Disciplina { Id = "subject-2", MateriaNome = "Matemática" };

        await InvokeTask(view, "CarregarDadosDisciplinaAsync", subject);

        Assert.Equal("7,25", view.GetFieldValue<Label>("label_media").Text);
        Assert.Equal("Alunos (2)", view.GetFieldValue<Label>("label_subtitulo_alunos").Text);
        Assert.Equal("4,00", view.GetFieldValue<Label>("label_menor_nota").Text);
        Assert.Equal("Bruno S.", view.GetFieldValue<Label>("label_menor_aluno").Text);
        Assert.Equal("9,00", view.GetFieldValue<Label>("label_maior_nota").Text);
        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("label_maior_aluno").Text);
        Assert.Equal(2, view.GetFieldValue<VerticalStackLayout>("layout_alunos").Children.Count);

        view.InvokeMember("DefinirDestaqueDistribuicao", response.Distribuicao);
        var doughnut = view.GetFieldValue<object>("doughnut_chart");
        Assert.Equal(1, (int)doughnut.GetType().GetProperty("ExplodeIndex")!.GetValue(doughnut)!);
        Assert.Equal(3, Assert.IsAssignableFrom<IList<Brush>>(doughnut.GetType().GetProperty("PaletteBrushes")!.GetValue(doughnut)).Count);
    }

    [Fact]
    public void ClassAndDisciplineSelectionsLoadTheirCurrentResults()
    {
        var discipline = new TurmasByAnoAndProfessor.Disciplina { Id = "subject-1", MateriaNome = "Matemática" };
        var classItem = new TurmasByAnoAndProfessor.Turma { Id = "class-1", Serie = 2, Disciplinas = [discipline] };
        var service = new FakeSimuladoService
        {
            TurmasByProfessor = new TurmasByAnoAndProfessor
            {
                Turmas = [new TurmasByAnoAndProfessor.Turma { Id = "class-3", Serie = 3 }, classItem]
            },
            DesempenhoByDisciplina = new DesempenhoAlunoByDisciplina
            {
                SimuladosContainer = new DesempenhoAlunoByDisciplina.SimuladoContainer { Media = 7 },
                Alunos = [new DesempenhoAlunoByDisciplina.DesempenhoAluno { Nome = "Ana", Media = 7 }]
            }
        };
        var view = new DesempenhoTurmaView(new FakeSettingsService(), new FakeNavigationService(), service) { TurmaId = "class-1" };

        view.InvokeMember("OnAppearing");

        var classes = Assert.IsAssignableFrom<IEnumerable<TurmasByAnoAndProfessor.Turma>>(view.GetFieldValue<SfChipGroup>("chirp_serie").ItemsSource);
        Assert.Collection(classes, turma => Assert.Equal(2, turma.Serie), turma => Assert.Equal(3, turma.Serie));
        Assert.Same(classItem, view.GetFieldValue<SfChipGroup>("chirp_serie").SelectedItem);

        view.InvokeMember("chirp_serie_SelectionChanged", null!, CreateSelection(classItem));
        Assert.Same(discipline, view.GetFieldValue<SfChipGroup>("chirp_disciplinas").SelectedItem);

        view.InvokeMember("chirp_disciplinas_SelectionChanged", null!, CreateSelection(discipline));
        Assert.Contains(service.Calls, call => call.Method == "getDesempenhoByDisciplina");

        var labelArgs = new ChartAxisLabelEventArgs("Ana Maria da Silva", 0);
        view.InvokeMember("ColumnChart_LabelCreated", null!, labelArgs);
        Assert.Equal("Ana S.", labelArgs.Label);
    }

    private static ChipsSelectionChangedEventArgs CreateSelection(object addedItem)
    {
        var eventArgs = (ChipsSelectionChangedEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(ChipsSelectionChangedEventArgs));
        var field = typeof(ChipsSelectionChangedEventArgs).GetField("<AddedItem>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(eventArgs, addedItem);
        return eventArgs;
    }

    private static Task InvokeTask(object view, string name, params object?[] arguments) =>
        (Task)view.InvokeMember(name, arguments)!;
}
