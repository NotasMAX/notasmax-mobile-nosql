using NotasMax.Views.Aluno;
using NotasMax.Views.Professor;
using NotasMax.Views.EsqueceuSenha;
using NotasMax.Tests.TestSupport;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using NotasMax.ContentsViews;
using NotasMax.Views;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class ViewConstructionTests
{
    public ViewConstructionTests(MauiTestAppFixture app)
    {
        Assert.NotNull(app.Application);
    }

    [Fact]
    public void HomeAlunoViewCanBeConstructedOnNeutralTarget()
    {
        var view = new HomeAlunoView(null!, null!, null!, null!);

        Assert.NotNull(view);
    }

    [Fact]
    public void RegisteredPagesAndContentViewsCanBeConstructedOnNeutralTarget()
    {
        var navigation = new FakeNavigationService();
        ISettingsService settings = new FakeSettingsService();
        IUsuarioService users = new FakeUsuarioService();
        ISimuladoService simulations = new FakeSimuladoService();

        object[] views =
        {
            new AppShell(navigation, settings),
            new LoginView(users, settings, navigation),
            new HomeProfessorView(navigation, settings, users, simulations),
            new TurmasView(simulations, settings, navigation),
            new DesempenhoTurmaView(settings, navigation, simulations),
            new PerfilView(settings, navigation, users, simulations),
            new PerfilProfessorView(settings, navigation, users, simulations),
            new MinhasMateriasView(simulations, settings),
            new DesempenhoAlunoView(simulations),
            new DadosPessoaisAlunoView(settings),
            new InformarEmailView(users),
            new InformarCodigoRecuperacaoView(users),
            new NovaSenhaView(users),
            new ExibirMateriaView(simulations, settings),
            new MeuDesempenhoView(settings, navigation, simulations),
            new MeuSimuladosView(settings, navigation, simulations),
            new ExibirSimuladoView(simulations),
            new CalendarioAlunoView(simulations),
            new CalendarioView(),
            new SimuladosView(),
            new AlunoItemContentView(),
            new AlunoNotaItemContentView(),
            new CalendarioItemContentView(),
            new ExibirSimuladoItemContentView(),
            new MateriaItemContentView(),
            new MeuSimuladoItemContentView(),
            new SimuladoItemContentView(),
            new TurmaItemContentView()
        };

        Assert.All(views, view => Assert.NotNull(view));
    }
}
