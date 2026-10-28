using Microsoft.Extensions.DependencyInjection;
using NotasMax.Services.NavigationService;
using NotasMax.Services.RequestProvider;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using NotasMax.Tests.TestSupport;
using NotasMax.Views.Aluno;
using NotasMax.Views.Professor;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class StartupTests
{
    public StartupTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public void AppCreatesWindowContainingItsShell()
    {
        var app = new App(new FakeNavigationService(), new FakeSettingsService());

        var window = (Window)app.InvokeMember("CreateWindow", new object?[] { null })!;

        Assert.IsType<AppShell>(window.Page);
        Assert.Equal(448, window.Width);
        Assert.Equal(700, window.Height);
    }

    [Fact]
    public void ShellShowsOnlyTheTabBarForTheCurrentRole()
    {
        var settings = new FakeSettingsService { UserRoleKey = "aluno" };
        var shell = new AppShell(new FakeNavigationService(), settings);

        Assert.True(shell.GetFieldValue<TabBar>("aluno_tabbar").IsVisible);
        Assert.False(shell.GetFieldValue<TabBar>("professor_tabbar").IsVisible);

        settings.UserRoleKey = " PROFESSOR ";
        shell.UpdateBottomMenu();

        Assert.False(shell.GetFieldValue<TabBar>("aluno_tabbar").IsVisible);
        Assert.True(shell.GetFieldValue<TabBar>("professor_tabbar").IsVisible);
    }

    [Fact]
    public void MauiProgramBuildsAnAppWithRegisteredServicesAndPages()
    {
        using var app = MauiProgram.CreateMauiApp();

        Assert.IsType<RequestProvider>(app.Services.GetRequiredService<IRequestProvider>());
        Assert.IsType<UsuarioService>(app.Services.GetRequiredService<IUsuarioService>());
        Assert.IsType<SettingsService>(app.Services.GetRequiredService<ISettingsService>());
        Assert.IsAssignableFrom<INavigationService>(app.Services.GetRequiredService<INavigationService>());
        Assert.IsType<SimuladoService>(app.Services.GetRequiredService<ISimuladoService>());
        Assert.NotNull(app.Services.GetRequiredService<HomeAlunoView>());
        Assert.NotNull(app.Services.GetRequiredService<HomeProfessorView>());
    }
}
