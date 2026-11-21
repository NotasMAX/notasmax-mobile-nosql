using NotasMax.Models.Usuarios;
using NotasMax.Models.Usuarios.Response;
using NotasMax.Exceptions;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Tests.TestSupport;
using NotasMax.Views;
using NotasMax.Views.EsqueceuSenha;
using System.Text.Json;
using System.Net;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class AuthenticationPageTests
{
    public AuthenticationPageTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Fact]
    public async Task LoginStoresStudentSessionAndNavigatesToStudentHome()
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
        var view = new TestableLoginView(users, settings, navigation);
        view.GetFieldValue<Entry>("entry_email").Text = "ana@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        await view.HandleLoginAsync();

        Assert.Equal("student-4", settings.UserIdKey);
        Assert.Equal("synthetic-token", settings.AuthAccessToken);
        Assert.Equal("aluno", settings.UserRoleKey);
        Assert.Equal(("aluno/inicio/homealunoview", true), Assert.Single(navigation.Navigations));
        Assert.Equal(0, view.InitialPasswordPromptCount);
    }

    [Theory]
    [InlineData("{\"usa_senha_inicial\":true}", true)]
    [InlineData("{\"usa_senha_inicial\":false}", false)]
    [InlineData("{}", false)]
    public void UserModelReadsInitialPasswordFlag(string json, bool expected)
    {
        var usuario = JsonSerializer.Deserialize<Usuario>(json);

        Assert.Equal(expected, usuario!.UsaSenhaInicial);
    }

    [Fact]
    public async Task InitialPasswordPromptCanOpenExistingRecoveryWithoutPersistingSession()
    {
        var settings = new FakeSettingsService { AuthAccessToken = string.Empty };
        var navigation = new FakeNavigationService();
        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken
            {
                Token = "synthetic-token",
                Usuario = new Usuario { Id = "teacher-2", TipoUsuario = "professor", UsaSenhaInicial = true }
            }
        };
        var view = new TestableLoginView(users, settings, navigation) { ChangePasswordSelected = true };
        FillCredentials(view);

        await view.HandleLoginAsync();

        Assert.Equal(1, view.InitialPasswordPromptCount);
        Assert.Equal(("///informaremail", false), Assert.Single(navigation.Navigations));
        Assert.Equal(string.Empty, settings.AuthAccessToken);
    }

    [Fact]
    public async Task DismissingInitialPasswordPromptContinuesToStudentHome()
    {
        var settings = new FakeSettingsService { AuthAccessToken = string.Empty };
        var navigation = new FakeNavigationService();
        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken
            {
                Token = "synthetic-token",
                Usuario = new Usuario { Id = "student-4", TipoUsuario = "aluno", UsaSenhaInicial = true }
            }
        };
        var view = new TestableLoginView(users, settings, navigation);
        FillCredentials(view);

        await view.HandleLoginAsync();

        Assert.Equal(1, view.InitialPasswordPromptCount);
        Assert.Equal(("aluno/inicio/homealunoview", true), Assert.Single(navigation.Navigations));
        Assert.Equal("synthetic-token", settings.AuthAccessToken);
    }

    [Fact]
    public async Task NonStudentProfileDoesNotShowInitialPasswordPrompt()
    {
        var settings = new FakeSettingsService { AuthAccessToken = string.Empty };
        var navigation = new FakeNavigationService();
        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken
            {
                Token = "synthetic-admin-token",
                Usuario = new Usuario { Id = "admin-1", TipoUsuario = "administrador", UsaSenhaInicial = true }
            }
        };
        var view = new TestableLoginView(users, settings, navigation);
        FillCredentials(view);

        await view.HandleLoginAsync();

        Assert.Equal(0, view.InitialPasswordPromptCount);
        Assert.Equal("synthetic-admin-token", settings.AuthAccessToken);
    }

    [Fact]
    public async Task LoginRequiresBothCredentialsBeforeCallingAuthentication()
    {
        var users = new FakeUsuarioService();
        var view = new TestableLoginView(users, new FakeSettingsService(), new FakeNavigationService());
        view.GetFieldValue<Entry>("entry_email").Text = "student@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "   ";

        await view.HandleLoginAsync();

        Assert.Equal(("Atenção", "Preencha os campos E-mail e Senha.", "Ok"), Assert.Single(view.Messages));
        Assert.Empty(users.AuthenticationRequests);
        Assert.Empty(view.NavigationService.Navigations);
    }

    [Fact]
    public async Task LoginShowsErrorWhenResponseHasNoUser()
    {
        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken { Usuario = null! }
        };
        var view = new TestableLoginView(users, new FakeSettingsService(), new FakeNavigationService());
        FillCredentials(view);

        await view.HandleLoginAsync();

        Assert.Equal(("Erro", "A autenticação retornou sem os dados do usuário.", "Ok"), Assert.Single(view.Messages));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "E-mail ou senha inválidos.")]
    [InlineData(HttpStatusCode.BadRequest, "Requisição inválida. Verifique os campos.")]
    public async Task LoginShowsExpectedMessageForApiErrors(HttpStatusCode statusCode, string expectedMessage)
    {
        var users = new FakeUsuarioService { ExceptionToThrow = new ApiException(statusCode, "synthetic API error") };
        var view = new TestableLoginView(users, new FakeSettingsService(), new FakeNavigationService());
        view.GetFieldValue<Entry>("entry_email").Text = "student@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        await view.HandleLoginAsync();

        Assert.Equal(("Erro", expectedMessage, "Ok"), Assert.Single(view.Messages));
    }

    [Fact]
    public async Task LoginShowsServerErrorForNetworkFailure()
    {
        var users = new FakeUsuarioService { ExceptionToThrow = new HttpRequestException("synthetic network failure") };
        var view = new TestableLoginView(users, new FakeSettingsService(), new FakeNavigationService());
        view.GetFieldValue<Entry>("entry_email").Text = "student@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        await view.HandleLoginAsync();

        Assert.Equal(("Erro", "Erro com o servidor, Tente novamente.", "Ok"), Assert.Single(view.Messages));
    }

    [Fact]
    public async Task LoginShowsUnexpectedErrorAndRejectsUnknownProfile()
    {
        var failedUsers = new FakeUsuarioService { ExceptionToThrow = new InvalidOperationException("synthetic failure") };
        var failedView = new TestableLoginView(failedUsers, new FakeSettingsService(), new FakeNavigationService());
        failedView.GetFieldValue<Entry>("entry_email").Text = "student@example.invalid";
        failedView.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        await failedView.HandleLoginAsync();

        Assert.Equal(("Erro", "Ocorreu um erro inesperado: synthetic failure", "Ok"), Assert.Single(failedView.Messages));

        var users = new FakeUsuarioService
        {
            TokenResponse = new UserToken
            {
                Token = "synthetic-token",
                Usuario = new Usuario { Id = "unknown-1", TipoUsuario = "unknown" }
            }
        };
        var navigation = new FakeNavigationService();
        var view = new TestableLoginView(users, new FakeSettingsService(), navigation);
        FillCredentials(view);

        await view.HandleLoginAsync();

        Assert.Equal(("Erro", "Perfil do usuário não reconhecido.", "Ok"), Assert.Single(view.Messages));
        Assert.Empty(navigation.Navigations);
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
    public async Task LoginNavigatesProfessorToProfessorHome()
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
        var view = new TestableLoginView(users, settings, navigation);
        view.GetFieldValue<Entry>("entry_email").Text = "teacher@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";

        await view.HandleLoginAsync();

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

    private static void FillCredentials(LoginView view)
    {
        view.GetFieldValue<Entry>("entry_email").Text = "student@example.invalid";
        view.GetFieldValue<Entry>("entry_senha").Text = "synthetic-password";
    }

    private sealed class TestableLoginView(
        FakeUsuarioService userService,
        FakeSettingsService settingsService,
        FakeNavigationService navigationService)
        : LoginView(userService, settingsService, navigationService)
    {
        public bool ChangePasswordSelected { get; set; }
        public int InitialPasswordPromptCount { get; private set; }

        internal override Task<bool> ConfirmInitialPasswordChangeAsync()
        {
            InitialPasswordPromptCount++;
            return Task.FromResult(ChangePasswordSelected);
        }

        public FakeNavigationService NavigationService { get; } = navigationService;
        public List<(string Title, string Message, string Accept)> Messages { get; } = new();

        internal override Task ShowMessageAsync(string title, string message, string accept)
        {
            Messages.Add((title, message, accept));
            return Task.CompletedTask;
        }
    }
}
