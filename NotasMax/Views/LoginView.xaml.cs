using NotasMax.Exceptions;
using NotasMax.Models.Usuarios;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Usuarios;
using NotasMax.Views.EsqueceuSenha;
using System.Diagnostics;
using System.Net;

namespace NotasMax.Views;

[QueryProperty(nameof(Logout), "Logout")] // rebece paremetro na rota, exemplo: "Login?Logout=true"
public partial class LoginView : ContentPage
{
    public bool Logout { get; set; } // salva o valor do parametro recebido na rota
    private readonly IUsuarioService _userService;
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;

    private bool isEyeOpen = false;
    public LoginView(IUsuarioService userService, ISettingsService settingsService, INavigationService navigationService)
    {
        _userService = userService;
        _settingsService = settingsService;
        _navigationService = navigationService;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Logout) // se o parametro recebido for true, limpa o token de autenticação
            _settingsService.AuthAccessToken = string.Empty;
    }

    void OnLabelEsqueceuSenhaTapped(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("///informaremail");
    }

    private void ToggleEye_Clicked(object sender, EventArgs e)
    {
        isEyeOpen = !isEyeOpen;
        entry_senha.IsPassword = !isEyeOpen;
        ToggleEye.Source = isEyeOpen ? "eye_closed.png" : "eye_open.png";
    }

    private async void ButtonEntrar_Clicked(object sender, EventArgs e)
    {
        await HandleLoginAsync();
    }

    internal async Task HandleLoginAsync()
    {
        var auth = new UsuarioAuth()
        {
            Email = entry_email.Text,
            Senha = entry_senha.Text,
        };

        if (string.IsNullOrWhiteSpace(auth.Email) || string.IsNullOrWhiteSpace(auth.Senha))
        {
            await ShowMessageAsync("Atenção", "Preencha os campos E-mail e Senha.", "Ok");
            return;
        }

        try
        {
            var response = await _userService.Authenticate(auth);

            if (response?.Usuario is null)
            {
                await ShowMessageAsync("Erro", "A autenticação retornou sem os dados do usuário.", "Ok");
                return;
            }

            var tipoUsuario = response.Usuario.TipoUsuario?.Trim().ToLowerInvariant();

            if (response.Usuario.UsaSenhaInicial && (tipoUsuario is "aluno" or "professor"))
            {
                var alterarSenha = await ConfirmInitialPasswordChangeAsync();
                if (alterarSenha)
                {
                    await _navigationService.NavigationAsync("///informaremail", false);
                    return;
                }
            }

            // Só persiste a sessão depois que o usuário decide continuar no app.
            _settingsService.Set(response);

            if (Shell.Current is AppShell shell)
                shell.UpdateBottomMenu();

            switch (tipoUsuario)
            {
                case "aluno":
                    await _navigationService.NavigationAsync("aluno/inicio/homealunoview", true);
                    break;

                case "professor":
                    await _navigationService.NavigationAsync("professor/inicio/homeprofessor", true);
                    break;

                case "administrador":
                    await ShowMessageAsync(
                        "Aviso",
                        "Você está realizando o login como administrador. No momento o aplicativo não conta com tela para administrador.",
                        "Ok");
                    break;

                default:
                    await ShowMessageAsync("Erro", "Perfil do usuário não reconhecido.", "Ok");
                    break;
            }

        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await ShowMessageAsync("Erro", "E-mail ou senha inválidos.", "Ok");
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            await ShowMessageAsync("Erro", "Requisição inválida. Verifique os campos.", "Ok");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
#if DEBUG
            Debug.WriteLine($"{ex.GetType().Name}: {ex.Message}");
#endif
            await ShowMessageAsync("Erro", "Erro com o servidor, Tente novamente.", "Ok");
        }

        catch (Exception ex)
        {
            await ShowMessageAsync("Erro", $"Ocorreu um erro inesperado: {ex.Message}", "Ok");
        }
    }

    internal virtual Task<bool> ConfirmInitialPasswordChangeAsync() =>
        DisplayAlertAsync(
            "Proteja sua conta",
            "Para manter sua conta segura, recomendamos que você atualize sua senha. Deseja fazer isso agora?",
            "Alterar senha",
            "Agora não");

    internal virtual Task ShowMessageAsync(string title, string message, string accept) =>
        DisplayAlertAsync(title, message, accept);
}
