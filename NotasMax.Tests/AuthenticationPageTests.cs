using NotasMax.Models.Usuarios;
using NotasMax.Models.Usuarios.Response;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Tests.TestSupport;
using NotasMax.Views;
using NotasMax.Views.EsqueceuSenha;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class AuthenticationPageTests
{
    public AuthenticationPageTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public void LoginStoresStudentSessionAndNavigatesToStudentHome()
    {
        var settings = new FakeSettingsService();
        var navigation = new FakeNavigationService();
        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken
            {
                Token = "synthetic-token",
                Usuario = new Usuario
                {
                    Id = "student-4",
                    Nome = "Ana Silva",
                    Email = "ana@example.invalid",
                    TipoUsuario = "aluno"
                }
            }
        };
        var view = new LoginView(users, settings, navigation);
        view.GetFieldValue<Entry>("entry_email").Text = "ana@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        view.InvokeMember("ButtonEntrar_Clicked", null!, EventArgs.Empty);

        Assert.Equal("student-4", settings.UserIdKey);
        Assert.Equal("synthetic-token", settings.AuthAccessToken);
        Assert.Equal("aluno", settings.UserRoleKey);
        Assert.Equal(("aluno/inicio/homealunoview", true), Assert.Single(navigation.Navigations));
    }

    [Fact]
    public void LoginLogoutClearsSessionAndEyeButtonTogglesPasswordVisibility()
    {
        var settings = new FakeSettingsService { AuthAccessToken = "existing-token" };
        var view = new LoginView(new FakeUsuarioService(), settings, new FakeNavigationService()) { Logout = true };

        view.InvokeMember("OnAppearing");
        Assert.Equal(string.Empty, settings.AuthAccessToken);

        var password = view.GetFieldValue<Entry>("entry_senha");
        view.InvokeMember("ToggleEye_Clicked", null!, EventArgs.Empty);
        Assert.False(password.IsPassword);
        view.InvokeMember("ToggleEye_Clicked", null!, EventArgs.Empty);
        Assert.True(password.IsPassword);
    }

    [Fact]
    public void LoginNavigatesProfessorToProfessorHome()
    {
        var settings = new FakeSettingsService();
        var navigation = new FakeNavigationService();
        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken
            {
                Token = "synthetic-teacher-token",
                Usuario = new Usuario { Id = "teacher-2", TipoUsuario = "professor" }
            }
        };
        var view = new LoginView(users, settings, navigation);
        view.GetFieldValue<Entry>("entry_email").Text = "teacher@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        view.InvokeMember("ButtonEntrar_Clicked", null!, EventArgs.Empty);

        Assert.Equal("teacher-2", settings.UserIdKey);
        Assert.Equal(("professor/inicio/homeprofessor", true), Assert.Single(navigation.Navigations));
    }

    [Fact]
    public async Task RecoveryEmailAcceptsAWellFormedAddress()
    {
        var view = new InformarEmailView(new FakeUsuarioService());
        view.GetFieldValue<Entry>("entry_email").Text = "aluno@example.invalid";

        var isValid = await (Task<bool>)view.InvokeMember("ValidateEmail")!;

        Assert.True(isValid);
    }

    [Fact]
    public async Task RecoveryCodeAcceptsAProvidedCodeAndDecodesQueryEmail()
    {
        var view = new InformarCodigoRecuperacaoView(new FakeUsuarioService())
        {
            Email = "aluno%2Bteste%40example.invalid"
        };
        view.GetFieldValue<Entry>("entry_codigo").Text = "123456";

        var isValid = await (Task<bool>)view.InvokeMember("ValidateCode")!;
        view.InvokeMember("OnAppearing");

        Assert.True(isValid);
        Assert.Equal("aluno+teste@example.invalid", view.Email);
        Assert.Contains(view.Email, view.GetFieldValue<Label>("label_info").Text);
    }

    [Fact]
    public async Task NewPasswordAcceptsMatchingPasswordsAndTogglesBothFields()
    {
        var view = new NovaSenhaView(new FakeUsuarioService());
        var password = view.GetFieldValue<Entry>("entry_password");
        var confirmation = view.GetFieldValue<Entry>("entry_confirm_password");
        password.Text = "synthetic-password";
        confirmation.Text = "synthetic-password";

        var isValid = await (Task<bool>)view.InvokeMember("ValidatePassword")!;
        view.InvokeMember("ToggleEyePassword_Clicked", null!, EventArgs.Empty);
        view.InvokeMember("ToggleEyeConfirmPassword_Clicked", null!, EventArgs.Empty);

        Assert.True(isValid);
        Assert.False(password.IsPassword);
        Assert.False(confirmation.IsPassword);
    }
}
