using System.Globalization;
using NotasMax.Models.Simulados;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Usuarios;
using NotasMax.Services.Simulados;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using NotasMax.Views.Aluno;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class PageLogicTests
{
    private readonly MauiTestAppFixture _app;

    public PageLogicTests(MauiTestAppFixture app) => _app = app;

    [Fact]
    public void HomeAlunoShowsApiSummaryAndBuildsOrderedBimestreSeries()
    {
        var settings = new FakeSettingsService();
        var userService = new FakeUsuarioService { User = new NotasMax.Models.Usuarios.Usuario { Nome = "Ana Maria" } };
        var simuladoService = new FakeSimuladoService
        {
            ByAluno = new SimuladoAlunoResponse
            {
                MediaGeral = 7.26,
                MediaPorMateria = [new MediaMateriaSimulado { MateriaId = "math", Materia = "Matemática", Media = 7.5 }],
                Simulados =
                [
                    new SimuladoAluno { Numero = 2, Bimestre = 2, MediaSimulado = 8.5 },
                    new SimuladoAluno { Numero = 1, Bimestre = 1, MediaSimulado = 6.5 },
                    new SimuladoAluno { Numero = 3, Bimestre = 2, MediaSimulado = 9.0 }
                ]
            }
        };
        var navigation = new FakeNavigationService();
        var view = new HomeAlunoView(navigation, settings, userService, simuladoService);

        view.InvokeMember("OnAppearing");

        Assert.Equal(7.3d, double.Parse(view.GetFieldValue<Label>("txt_notaGeral").Text, CultureInfo.CurrentCulture));
        Assert.Equal("Ana Maria", view.GetFieldValue<Label>("txt_nomeUsuario").Text);
        Assert.Equal(new[] { "1º Bimestre", "2º Bimestre" }, Assert.IsAssignableFrom<IEnumerable<string>>(view.GetFieldValue<Picker>("picker_bimestre").ItemsSource));

        var picker = view.GetFieldValue<Picker>("picker_bimestre");
        picker.SelectedIndex = 1;
        view.InvokeMember("Picker_Bimestre_Changed", picker, EventArgs.Empty);
        var series = Assert.IsAssignableFrom<IEnumerable<GraficoSimuladoHomeAlunoData>>(view.GetFieldValue<object>("serie_evolucao").GetType().GetProperty("ItemsSource")!.GetValue(view.GetFieldValue<object>("serie_evolucao")));

        Assert.Collection(series,
            item => { Assert.Equal("Sim. 2", item.Label); Assert.Equal(8.5, item.Media); },
            item => { Assert.Equal("Sim. 3", item.Label); Assert.Equal(9.0, item.Media); });
        view.InvokeMember("OnLabelVerTudoMateria", null!, null!);
        Assert.Equal(("MinhasMaterias", false), navigation.Navigations.SingleOrDefault());
    }

    [Fact]
    public void HomeAlunoHandlesAnEmptyBimestreListAndConverterInputs()
    {
        var view = new HomeAlunoView(new FakeNavigationService(), new FakeSettingsService(), new FakeUsuarioService(), new FakeSimuladoService());
        var picker = view.GetFieldValue<Picker>("picker_bimestre");

        view.InvokeMember("OnAppearing");
        view.InvokeMember("Picker_Bimestre_Changed", picker, EventArgs.Empty);

        Assert.Empty((IEnumerable<string>)picker.ItemsSource);
        Assert.InRange((double)view.Convert(0.72d, typeof(double), null!, CultureInfo.InvariantCulture), 7.1999d, 7.2001d);
        Assert.Equal(0, view.Convert("0.72", typeof(double), null!, CultureInfo.InvariantCulture));
        Assert.Throws<NotImplementedException>(() => view.ConvertBack(1d, typeof(double), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    // Baseline only: this simple average is not an approved academic calculation rule.
    [Trait("Category", "Baseline non-normative")]
    public void ExibirMateriaCalculatesMeanFromMatchingSimulationResults()
    {
        var service = new FakeSimuladoService
        {
            ByAluno = new SimuladoAlunoResponse
            {
                Simulados =
                [
                    new SimuladoAluno { Numero = 2, Materias = [new MateriaSimuladoAluno { MateriaId = "math", Nota = 8 }] },
                    new SimuladoAluno { Numero = 1, Materias = [new MateriaSimuladoAluno { MateriaId = "other", Nota = 10 }] },
                    new SimuladoAluno { Numero = 1, Materias = [new MateriaSimuladoAluno { MateriaId = "math", Nota = 6 }] }
                ]
            }
        };
        var view = new ExibirMateriaView(service, new FakeSettingsService()) { MateriaId = "math", MateriaNome = "Matemática" };

        view.InvokeMember("OnAppearing");

        var mediaText = view.GetFieldValue<Label>("label_valor_media").Text;
        Assert.Equal(7d, double.Parse(mediaText, CultureInfo.CurrentCulture));
        Assert.Equal("Matemática", view.GetFieldValue<Label>("label_titulo_materia").Text);
    }

    [Fact]
    public void ExibirMateriaShowsZeroWhenNoResultMatches()
    {
        var service = new FakeSimuladoService { ByAluno = new SimuladoAlunoResponse { Simulados = [] } };
        var view = new ExibirMateriaView(service, new FakeSettingsService()) { MateriaId = "missing" };

        view.InvokeMember("OnAppearing");

        Assert.Equal(0d, double.Parse(view.GetFieldValue<Label>("label_valor_media").Text, CultureInfo.CurrentCulture));
    }

    [Fact]
    // Baseline only: current displayed averages do not establish the approved weighted-average policy.
    [Trait("Category", "Baseline non-normative")]
    public void MeuDesempenhoDisplaysAverageAndExtremesFromCurrentResponse()
    {
        using var culture = new TestCultureScope("pt-BR");
        var service = new FakeSimuladoService
        {
            DesempenhoAluno = new DesempenhoAluno
            {
                Simulados = [new DesempenhoAluno.Simulado { Media = 6 }, new DesempenhoAluno.Simulado { Media = 8 }],
                Materias = [new DesempenhoAluno.Materia { Nome = "Matemática", Media = 5 }, new DesempenhoAluno.Materia { Nome = "História", Media = 9 }]
            }
        };
        var view = new MeuDesempenhoView(new FakeSettingsService(), new FakeNavigationService(), service);

        view.InvokeMember("OnAppearing");

        Assert.Equal(7d, double.Parse(view.GetFieldValue<Label>("label_media").Text, CultureInfo.CurrentCulture));
        Assert.Equal("5,00", view.GetFieldValue<Label>("label_menor_nota").Text);
        Assert.Equal("Matemática", view.GetFieldValue<Label>("label_menor_materia").Text);
        Assert.Equal("9,00", view.GetFieldValue<Label>("label_maior_nota").Text);
        Assert.Equal("História", view.GetFieldValue<Label>("label_maior_materia").Text);
    }

    [Fact]
    public void MeuDesempenhoReturnsWithoutUpdatingWhenTheServiceThrows()
    {
        var service = new FakeSimuladoService { ExceptionToThrow = new InvalidOperationException("synthetic failure") };
        var view = new MeuDesempenhoView(new FakeSettingsService(), new FakeNavigationService(), service);

        view.InvokeMember("OnAppearing");

        Assert.Equal("0,0", view.GetFieldValue<Label>("label_media").Text);
    }

    [Fact]
    public void MeuSimuladosCountsPastAndFutureDates()
    {
        var service = new FakeSimuladoService
        {
            SimuladosAluno = new DesempenhoAlunoBySimulados
            {
                Simulados =
                [
                    new DesempenhoAlunoBySimulados.Simulado { Numero = 1, DataRealizacao = DateTime.Now.AddDays(-1) },
                    new DesempenhoAlunoBySimulados.Simulado { Numero = 2, DataRealizacao = DateTime.Now.AddDays(1) }
                ]
            }
        };
        var view = new MeuSimuladosView(new FakeSettingsService(), new FakeNavigationService(), service);

        view.InvokeMember("OnAppearing");

        Assert.Equal("1", view.GetFieldValue<Label>("label_numero_simulados_realizados").Text);
        Assert.Equal("1", view.GetFieldValue<Label>("label_numero_simulados_pendentes").Text);
    }

    [Fact]
    public void MinhasMateriasBuildsCardsInTwoColumns()
    {
        var service = new FakeSimuladoService
        {
            ByAluno = new SimuladoAlunoResponse
            {
                MediaPorMateria =
                [
                    new MediaMateriaSimulado { MateriaId = "m1", Materia = "Matemática" },
                    new MediaMateriaSimulado { MateriaId = "m2", Materia = "História" },
                    new MediaMateriaSimulado { MateriaId = "m3", Materia = "Geografia" }
                ]
            }
        };
        var view = new MinhasMateriasView(service, new FakeSettingsService());

        view.InvokeMember("OnAppearing");

        Assert.Equal(3, view.GetFieldValue<Grid>("grid_materias").Children.Count);
        Assert.Equal(2, view.GetFieldValue<Grid>("grid_materias").RowDefinitions.Count);
    }
}
