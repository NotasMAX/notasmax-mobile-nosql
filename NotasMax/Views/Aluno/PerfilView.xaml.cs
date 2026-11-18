using NotasMax.Helpers;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using System.Diagnostics;

namespace NotasMax.Views.Aluno;

public partial class PerfilView : ContentPage
{
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly IUsuarioService _usuarioService;
    private readonly ISimuladoService _simuladoService;

    public PerfilView(
        ISettingsService settingsService,
        INavigationService navigationService,
        IUsuarioService usuarioService,
        ISimuladoService simuladoService)
    {
        _settingsService = settingsService;
        _navigationService = navigationService;
        _usuarioService = usuarioService;
        _simuladoService = simuladoService;

        InitializeComponent(); // labels só existem a partir daqui
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDados();
    }

    private async Task CarregarDados()
    {
        try
        {
            // Dados do usuário já salvos no SettingsService
            var nome = _settingsService.UserNameKey;
            label_nome.Text = nome;
            label_iniciais.Text = TextoHelper.PegarIniciais(nome);

            // Carregar desempenho para obter nota geral, qtd simulados e matérias
            var desempenho = await _simuladoService.GetDesempenhoAluno();

            if (desempenho != null)
            {
                if (desempenho.Simulados?.Count > 0)
                {
                    double notaGeral = desempenho.Simulados.Average(s => s.Media);
                    label_nota_geral.Text = notaGeral.ToString("N1");
                    label_qtd_simulados.Text = desempenho.Simulados.Count.ToString();
                }
                else
                {
                    label_nota_geral.Text = "--";
                    label_qtd_simulados.Text = "0";
                }

                label_qtd_materias.Text = desempenho.Materias?.Count.ToString() ?? "0";
            }

            // label_turma: o modelo Usuario não expõe série/turma ainda,
            // então fica vazio até a API retornar esse campo
            label_turma.Text = string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar perfil: {ex.Message}");
        }
    }

    private async void DadosPessoais_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("DadosPessoaisAluno");
    }

    private async void MinhasMaterias_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("MinhasMaterias");
    }

    private async void LerQrCode_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("LerQrCode");
    }

    private void Sair_Tapped(object sender, TappedEventArgs e)
    {
        _navigationService.NavigationAsync("Login?Logout=true", true);
    }
}
