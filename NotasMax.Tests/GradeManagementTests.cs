using System.Reflection;
using System.Runtime.CompilerServices;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using NotasMax.Views.Professor;
using Syncfusion.Maui.Toolkit.Chips;
using Xunit;
using ChipSelectionChangedEventArgs = Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class GradeManagementTests
{
    public GradeManagementTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public void GradeManagementLoadsAndSortsTheClassDisciplineSimulationAndStudentHierarchy()
    {
        var math = new GestaoNotasViewModel.Disciplina
        {
            Id = "subject-math",
            MateriaNome = "Matemática",
            Simulados =
            [
                new GestaoNotasViewModel.Simulado { Id = "sim-2", Numero = 2 },
                new GestaoNotasViewModel.Simulado { Id = "sim-1", Numero = 1 }
            ]
        };
        var history = new GestaoNotasViewModel.Disciplina { Id = "subject-history", MateriaNome = "História" };
        var firstClass = new GestaoNotasViewModel.Turma
        {
            Id = "class-1",
            Serie = 1,
            Disciplinas = [math, history]
        };
        var secondClass = new GestaoNotasViewModel.Turma { Id = "class-3", Serie = 3 };
        var simulationService = new FakeSimuladoService
        {
            GestaoNotas = new GestaoNotasViewModel { Turmas = [secondClass, firstClass] },
            GestaoNotasSimulado = new GestaoNotasSimuladoViewModel
            {
                SimuladoId = "sim-1",
                DisciplinaId = "subject-math",
                Alunos =
                [
                    new GestaoNotasSimuladoViewModel.Aluno { Id = "b", Nome = "Bruno", Acertos = 7, QuantidadeQuestoes = 10 },
                    new GestaoNotasSimuladoViewModel.Aluno { Id = "a", Nome = "Ana", Acertos = 8, QuantidadeQuestoes = 10 }
                ]
            }
        };
        var view = new GestaoNotasView(new FakeSettingsService(), new FakeNavigationService(), simulationService);

        view.InvokeMember("OnAppearing");
        Assert.Collection(Assert.IsAssignableFrom<IEnumerable<GestaoNotasViewModel.Turma>>(view.GetFieldValue<SfChipGroup>("chirp_serie").ItemsSource),
            turma => Assert.Equal(1, turma.Serie),
            turma => Assert.Equal(3, turma.Serie));

        view.InvokeMember("chirp_serie_SelectionChanged", null!, CreateSelection(firstClass));
        Assert.Collection(Assert.IsAssignableFrom<IEnumerable<GestaoNotasViewModel.Disciplina>>(view.GetFieldValue<SfChipGroup>("chirp_disciplinas").ItemsSource),
            disciplina => Assert.Equal("História", disciplina.MateriaNome),
            disciplina => Assert.Equal("Matemática", disciplina.MateriaNome));

        view.GetFieldValue<SfChipGroup>("chirp_disciplinas").SelectedItem = math;
        view.InvokeMember("chirp_disciplinas_SelectionChanged", null!, CreateSelection(math));
        Assert.Collection(Assert.IsAssignableFrom<IEnumerable<GestaoNotasViewModel.Simulado>>(view.GetFieldValue<SfChipGroup>("chirp_simulados").ItemsSource),
            simulado => Assert.Equal(1, simulado.Numero),
            simulado => Assert.Equal(2, simulado.Numero));

        view.InvokeMember("chirp_simulados_SelectionChanged", null!, CreateSelection(math.Simulados[1]));

        var students = view.GetFieldValue<VerticalStackLayout>("layout_alunos").Children;
        Assert.Equal(2, students.Count);
        Assert.Equal("GetGestaoNotas", simulationService.Calls[0].Method);
        Assert.Equal("GetGestaoNotasSimulado", simulationService.Calls[^1].Method);
    }

    [Fact]
    public void SavingBeforeLoadingASelectedSimulationDoesNotSendGrades()
    {
        var service = new FakeSimuladoService();
        var view = new GestaoNotasView(new FakeSettingsService(), new FakeNavigationService(), service);

        view.InvokeMember("button_salvar_Clicked", null!, EventArgs.Empty);

        Assert.Empty(service.SavedGrades);
    }

    [Fact]
    public void GradeSelectionReturnsWhenDetailRequestFails()
    {
        var service = new FakeSimuladoService { ExceptionToThrow = new InvalidOperationException("synthetic failure") };
        var view = new GestaoNotasView(new FakeSettingsService(), new FakeNavigationService(), service);
        var discipline = new GestaoNotasViewModel.Disciplina { Id = "subject-9", MateriaNome = "História" };
        view.GetFieldValue<SfChipGroup>("chirp_disciplinas").SelectedItem = discipline;

        view.InvokeMember("chirp_simulados_SelectionChanged", null!, CreateSelection(new GestaoNotasViewModel.Simulado { Id = "sim-9", Numero = 2 }));

        Assert.Empty(view.GetFieldValue<VerticalStackLayout>("layout_alunos").Children);
    }

    private static ChipSelectionChangedEventArgs CreateSelection(object addedItem)
    {
        var eventArgs = (ChipSelectionChangedEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(ChipSelectionChangedEventArgs));
        var addedItemField = typeof(ChipSelectionChangedEventArgs).GetField("<AddedItem>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(addedItemField);
        addedItemField!.SetValue(eventArgs, addedItem);
        return eventArgs;
    }
}
