using NotasMax.Models.Simulados;
using NotasMax.Models.Usuarios;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using System.Globalization;

namespace NotasMax.Views.Aluno;

public partial class HomeAlunoView : ContentPage, IValueConverter
{
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IUsuarioService _usuarioService;
    private readonly ISimuladoService _simuladoService;

    private Usuario? _usuario;
    private SimuladoAlunoResponse _simulados = new();

    public HomeAlunoView(INavigationService navigationService, ISettingsService settingsService, IUsuarioService usuarioService, ISimuladoService simuladoService)
    {
        _navigationService = navigationService;
        _settingsService = settingsService;
        _usuarioService = usuarioService;
        _simuladoService = simuladoService;

        InitializeComponent();
    }

    // Carrega toda vez que entrar nesta tela
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _usuario = await _usuarioService.GetUsuarioById(
                _settingsService.UserIdKey,
                _settingsService.AuthAccessToken
            );

            _simulados = await _simuladoService.GetByAluno(
                _settingsService.UserIdKey,
                _settingsService.AuthAccessToken
            );

            txt_notaGeral.Text = _simulados.MediaGeral.ToString("F1");

            string nameUser = _usuario?.Nome ?? "Usuário";

            txt_nomeUsuario.Text = nameUser;
            txt_inicialUsuario.Text = !string.IsNullOrEmpty(nameUser)
                ? nameUser[0].ToString()
                : "?";

            collection_materias.ItemsSource = _simulados.MediaPorMateria;
            img_seta.Source = _simulados.MediaGeral >= 7.0
                            ? "seta_subindo_desempenho.png"
                            : "seta_descendo_desempenho.png";

            CarregarPicker(); // Carregar picker do grafico
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro", $"Sem conexão com o servidor. \nMessage:{ex.Message}", "Ok");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", $"Erro inesperado: {ex.Message}", "Ok");
        }
    } // Fim OnAppearing

    // Converte valor da media da nota por disciplina para ficar compativel com o ProgressBar - Aceita valor fracionado de 0.0 a 1.0
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double nota)
            return nota * 10.0;
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();


    // Ver todas as materias
    private async void OnLabelVerTudoMateria(object sender, TappedEventArgs e)
    {
        await _navigationService.NavigationAsync("MinhasMaterias", false);
    }

    // Ir para tela de desempnho de uma materia 
    private async void TapGestureRecognizer_VerMateria(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string materiaId && !string.IsNullOrWhiteSpace(materiaId))
        {
            var materia = _simulados.MediaPorMateria
                .FirstOrDefault(m => m.MateriaId == materiaId);

            if (materia is null)
                return;

            await Shell.Current.GoToAsync(
                $"Materia?materiaId={Uri.EscapeDataString(materiaId)}&nome={Uri.EscapeDataString(materia.Materia)}");
        }
    }

    // Botao para ir na tela de perfil
    private async void Avatar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//aluno/perfil/perfilaluno");
    }

    // Grafico
    private void CarregarPicker()
    {
        // Pega os bimestres únicos e ordena
        var bimestres = _simulados.Simulados
            .Select(s => s.Bimestre)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        picker_bimestre.ItemsSource = bimestres
            .Select(b => $"{b}º Bimestre")
            .ToList();

        // Seleciona o primeiro bimestre automaticamente
        if (bimestres.Count > 0)
        {
            picker_bimestre.SelectedIndex = 0;
            AtualizarGrafico(bimestres[0]);
        }
    }

    private void Picker_Bimestre_Changed(object sender, EventArgs e)
    {
        if (picker_bimestre.SelectedIndex < 0) return;

        // Extrai o número do bimestre do texto "1º Bimestre" → 1
        var bimestres = _simulados.Simulados
            .Select(s => s.Bimestre)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        var bimestreSelecionado = bimestres[picker_bimestre.SelectedIndex];
        AtualizarGrafico(bimestreSelecionado);
    }

    private void AtualizarGrafico(int bimestre)
    {
        var dados = _simulados.Simulados
            .Where(s => s.Bimestre == bimestre)
            .OrderBy(s => s.Numero)
            .Select(s => new GraficoSimuladoHomeAlunoData
            {
                Label = $"Sim. {s.Numero}",
                Media = s.MediaSimulado
            })
            .ToList();

        serie_evolucao.ItemsSource = dados;
    }
}