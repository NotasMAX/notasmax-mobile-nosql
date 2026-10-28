using NotasMax.ContentsViews;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class ContentViewBindingTests
{
    public ContentViewBindingTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public void AlunoCardFormatsNameInitialsAndGradeFromBindingContext()
    {
        var view = new AlunoItemContentView
        {
            BindingContext = new DesempenhoAlunoByDisciplina.DesempenhoAluno
            {
                Nome = "Ana Maria da Silva",
                Media = 8.25
            }
        };

        Assert.Equal("8,25", view.GetFieldValue<Label>("label_nota_aluno").Text);
        Assert.Equal("Ana Maria da Silva", view.GetFieldValue<Label>("label_nome_aluno").Text);
        Assert.Equal("AS", view.GetFieldValue<Label>("label_iniciais_aluno").Text);
    }

    [Fact]
    public void GradeEntryUpdatesAcertosAndClampsBelowZeroAndAboveQuestionCount()
    {
        var aluno = new GestaoNotasSimuladoViewModel.Aluno
        {
            Nome = "Aluno de Teste",
            QuantidadeQuestoes = 10,
            Acertos = 5,
            Nota = 5
        };
        var view = new AlunoNotaItemContentView { BindingContext = aluno };
        var entry = view.GetFieldValue<Entry>("entry_acertos");

        entry.Text = "7";
        Assert.Equal(7, aluno.Acertos);
        Assert.Equal(7d, aluno.Nota);

        entry.Text = "-1";
        Assert.Equal(0, aluno.Acertos);
        Assert.Equal(0d, aluno.Nota);

        entry.Text = "11";
        Assert.Equal(10, aluno.Acertos);
        Assert.Equal(10d, aluno.Nota);
    }

    [Theory]
    [InlineData("objetivo", "Objetivo")]
    [InlineData("discursivo", "Discursivo")]
    public void CalendarCardDisplaysDateTypeAndNumber(string tipo, string tipoExibido)
    {
        var view = new CalendarioItemContentView
        {
            BindingContext = new CalendarioByAluno.CalendarioSimulado
            {
                Numero = 4,
                Tipo = tipo,
                DataRealizacao = new DateTime(2026, 3, 15)
            }
        };

        Assert.Equal("Simulado 4", view.GetFieldValue<Label>("label_numero_simulado").Text);
        Assert.Equal(tipoExibido, view.GetFieldValue<Label>("label_tipo").Text);
        Assert.Contains("15", view.GetFieldValue<Label>("label_data_realizacao").Text);
    }

    [Fact]
    public void SimulationDisciplineCardDisplaysSubjectAndGrade()
    {
        var view = new ExibirSimuladoItemContentView
        {
            BindingContext = new DesempenhoAlunoBySimulado.Disciplina { Nome = "História", Nota = 6.5 }
        };

        Assert.Equal("História", view.GetFieldValue<Label>("label_nome_disciplona").Text);
        Assert.Equal("6,50", view.GetFieldValue<Label>("label_valor_nota").Text);
    }

    [Fact]
    public void SimulationCardFormatsNumberDateAndGrade()
    {
        var view = new SimuladoItemContentView
        {
            BindingContext = new NotaSimulado { Numero = "3", Nota = 7.25, Data = new DateTime(2026, 6, 9) }
        };

        Assert.Equal("Simulado 3", view.GetFieldValue<Label>("label_nome_simulado").Text);
        Assert.Equal("7,25", view.GetFieldValue<Label>("label_valor_nota").Text);
        Assert.Contains("2026", view.GetFieldValue<Label>("label_data").Text);
    }

    [Fact]
    public void RealizedAndUpcomingSimulationCardsUseTheirCurrentDisplayBranches()
    {
        var realized = new MeuSimuladoItemContentView
        {
            BindingContext = new DesempenhoAlunoBySimulados.Simulado
            {
                Numero = 2,
                Media = 8.5,
                DataRealizacao = DateTime.Now.AddDays(-1)
            }
        };
        var upcoming = new MeuSimuladoItemContentView
        {
            BindingContext = new DesempenhoAlunoBySimulados.Simulado
            {
                Numero = 3,
                Media = 0,
                DataRealizacao = DateTime.Now.AddDays(1)
            }
        };

        Assert.Equal("8,50", realized.GetFieldValue<Label>("label_valor_media").Text);
        Assert.True(realized.GetFieldValue<VisualElement>("container_media").IsVisible);
        Assert.False(upcoming.GetFieldValue<VisualElement>("container_media").IsVisible);
        Assert.Equal("Simulado 3", upcoming.GetFieldValue<Label>("label_numero_simulado").Text);
    }

    [Fact]
    public void TurmaCardDisplaysItsAverageAndStudentCount()
    {
        var view = new TurmaItemContentView
        {
            BindingContext = new TurmasByAnoAndProfessor.Turma { Serie = 2, Media = 7.4, QuantidadeAlunos = 31 }
        };

        Assert.Equal("7,40", view.GetFieldValue<Label>("label_valor_media").Text);
        Assert.Equal("31 alunos", view.GetFieldValue<Label>("label_quantidade_alunos").Text);
    }

    [Fact]
    public void ComponentActionsReturnSafelyWhenTheirBindingOrShellIsUnavailable()
    {
        var alunoCard = new AlunoItemContentView();
        alunoCard.InvokeMember("TurmaItem_Tapped", null!, new TappedEventArgs(null));

        var pendingSimulation = new MeuSimuladoItemContentView
        {
            BindingContext = new DesempenhoAlunoBySimulados.Simulado
            {
                DataRealizacao = DateTime.Now.AddDays(1)
            }
        };
        pendingSimulation.InvokeMember("Button_Tapped", null!, new TappedEventArgs(null));

        var turmaCard = new TurmaItemContentView
        {
            BindingContext = new TurmasByAnoAndProfessor.Turma { Id = "class-1" }
        };
        turmaCard.InvokeMember("TurmaItem_Tapped", null!, new TappedEventArgs(null));

        Assert.NotNull(turmaCard);
    }
}
