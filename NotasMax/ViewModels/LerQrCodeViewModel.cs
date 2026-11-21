using CommunityToolkit.Mvvm.ComponentModel;
using NotasMax.Models.Presencas;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Presencas;
using System.Windows.Input;

namespace NotasMax.ViewModels;

public enum EstadoLeituraQrCode
{
    SolicitandoPermissao,
    Lendo,
    Processando,
    PermissaoNegada,
    Sucesso,
    JaRegistrada,
    TurmaDiferente,
    QrInvalido,
    SemConexao,
    ErroServidor
}

public partial class LerQrCodeViewModel : ObservableObject
{
    private const int TamanhoMinimoToken = 20;
    private const int TamanhoMaximoToken = 8192;

    private readonly IPresencaService _presencaService;
    private readonly INavigationService _navigationService;
    private EstadoLeituraQrCode _estado = EstadoLeituraQrCode.SolicitandoPermissao;
    private string _mensagem = "Solicitando permissão da câmera...";
    private string _titulo = string.Empty;
    private string _detalhe = string.Empty;
    private bool _cameraAtiva;
    private bool _leituraPausada = true;
    private bool _processando;

    public LerQrCodeViewModel(IPresencaService presencaService, INavigationService navigationService)
    {
        _presencaService = presencaService;
        _navigationService = navigationService;

        AbrirConfiguracoesCommand = new Command(AppInfo.ShowSettingsUI);
        VoltarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ConcluirCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        LerNovamenteCommand = new Command(LerNovamente);
        TentarNovamenteCommand = new Command(LerNovamente);
    }

    public EstadoLeituraQrCode Estado
    {
        get => _estado;
        private set
        {
            if (SetProperty(ref _estado, value))
            {
                OnPropertyChanged(nameof(IsSolicitandoPermissao));
                OnPropertyChanged(nameof(IsCameraVisivel));
                OnPropertyChanged(nameof(IsPermissaoNegada));
                OnPropertyChanged(nameof(IsProcessando));
                OnPropertyChanged(nameof(IsSucesso));
                OnPropertyChanged(nameof(IsAviso));
                OnPropertyChanged(nameof(IsErro));
                OnPropertyChanged(nameof(PodeLerOutroQr));
                OnPropertyChanged(nameof(PodeTentarNovamente));
            }
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public string Titulo
    {
        get => _titulo;
        private set => SetProperty(ref _titulo, value);
    }

    public string Detalhe
    {
        get => _detalhe;
        private set => SetProperty(ref _detalhe, value);
    }

    public bool CameraAtiva
    {
        get => _cameraAtiva;
        private set => SetProperty(ref _cameraAtiva, value);
    }

    public bool LeituraPausada
    {
        get => _leituraPausada;
        private set => SetProperty(ref _leituraPausada, value);
    }

    public bool IsSolicitandoPermissao => Estado == EstadoLeituraQrCode.SolicitandoPermissao;
    public bool IsCameraVisivel => Estado == EstadoLeituraQrCode.Lendo;
    public bool IsPermissaoNegada => Estado == EstadoLeituraQrCode.PermissaoNegada;
    public bool IsProcessando => Estado == EstadoLeituraQrCode.Processando;
    public bool IsSucesso => Estado == EstadoLeituraQrCode.Sucesso;
    public bool IsAviso => Estado == EstadoLeituraQrCode.JaRegistrada;
    public bool IsErro => Estado is EstadoLeituraQrCode.TurmaDiferente
        or EstadoLeituraQrCode.QrInvalido
        or EstadoLeituraQrCode.SemConexao
        or EstadoLeituraQrCode.ErroServidor;
    public bool PodeLerOutroQr => Estado == EstadoLeituraQrCode.TurmaDiferente;
    public bool PodeTentarNovamente => Estado is EstadoLeituraQrCode.QrInvalido
        or EstadoLeituraQrCode.SemConexao
        or EstadoLeituraQrCode.ErroServidor;

    public ICommand AbrirConfiguracoesCommand { get; }
    public ICommand VoltarCommand { get; }
    public ICommand ConcluirCommand { get; }
    public ICommand LerNovamenteCommand { get; }
    public ICommand TentarNovamenteCommand { get; }

    public void IniciarSolicitacaoPermissao()
    {
        _processando = false;
        Estado = EstadoLeituraQrCode.SolicitandoPermissao;
        Mensagem = "Solicitando permissão da câmera...";
        Titulo = string.Empty;
        Detalhe = string.Empty;
        DesativarCamera();
    }

    public void PermissaoConcedida()
    {
        _processando = false;
        Estado = EstadoLeituraQrCode.Lendo;
        Mensagem = "Aponte a câmera para o QR Code na porta da sala.";
        Titulo = string.Empty;
        Detalhe = string.Empty;
        CameraAtiva = true;
        LeituraPausada = false;
    }

    public void PermissaoNegada()
    {
        _processando = false;
        Estado = EstadoLeituraQrCode.PermissaoNegada;
        Mensagem = "Precisamos da câmera para ler o QR Code de presença";
        Titulo = string.Empty;
        Detalhe = string.Empty;
        DesativarCamera();
    }

    public async Task ProcessarLeituraAsync(string conteudo)
    {
        if (Estado != EstadoLeituraQrCode.Lendo || _processando)
            return;

        _processando = true;
        DesativarCamera();

        if (!TokenTemFormatoJwt(conteudo))
        {
            Estado = EstadoLeituraQrCode.QrInvalido;
            Titulo = "QR Code inválido";
            Mensagem = "QR Code inválido";
            Detalhe = string.Empty;
            _processando = false;
            return;
        }

        Estado = EstadoLeituraQrCode.Processando;
        Titulo = "Registrando presença...";
        Mensagem = "Registrando presença...";
        Detalhe = string.Empty;

        try
        {
            var resultado = await _presencaService.RegistrarPresenca(conteudo.Trim());

            switch (resultado.Status)
        {
            case StatusPresenca.Sucesso:
                Estado = EstadoLeituraQrCode.Sucesso;
                Titulo = "Presença registrada!";
                Mensagem = resultado.Simulado is null
                    ? "Presença registrada!"
                    : $"Simulado {resultado.Simulado.Numero} • {resultado.Simulado.Tipo}";
                Detalhe = "Presença registrada!";
                break;

            case StatusPresenca.JaRegistrada:
                Estado = EstadoLeituraQrCode.JaRegistrada;
                Titulo = "Presença já registrada";
                Mensagem = "Você já registrou presença neste simulado.";
                Detalhe = string.Empty;
                DesativarCamera();
                break;

            case StatusPresenca.TurmaDiferente:
                Estado = EstadoLeituraQrCode.TurmaDiferente;
                Titulo = "Este QR Code não faz parte da sua turma.";
                Mensagem = Titulo;
                Detalhe = string.Empty;
                break;

            case StatusPresenca.QrInvalido:
                Estado = EstadoLeituraQrCode.QrInvalido;
                Titulo = string.IsNullOrWhiteSpace(resultado.Mensagem) ? "QR Code inválido" : resultado.Mensagem;
                Mensagem = Titulo;
                Detalhe = string.Empty;
                break;

            case StatusPresenca.SemConexao:
                Estado = EstadoLeituraQrCode.SemConexao;
                Titulo = "Não foi possível conectar. Verifique sua internet e tente novamente.";
                Mensagem = Titulo;
                Detalhe = string.Empty;
                break;

            case StatusPresenca.ErroServidor:
                Estado = EstadoLeituraQrCode.ErroServidor;
                Titulo = "Não foi possível registrar a presença. Tente novamente em instantes.";
                Mensagem = Titulo;
                Detalhe = string.Empty;
                break;

            case StatusPresenca.SessaoExpirada:
                break;
        }

        if (resultado.Status == StatusPresenca.SessaoExpirada)
            await _navigationService.NavigationAsync("Login?Logout=true", absolute: true);
            
        }
        catch (Exception ex)
        {
            Estado = EstadoLeituraQrCode.ErroServidor;
            Titulo = "Não foi possível registrar a presença. Tente novamente em instantes.";
            Mensagem = Titulo;
            Detalhe = string.Empty;
            Console.WriteLine($"Erro ao registrar presença: {ex.Message}");
        }
        finally
        {
            _processando = false;
        }    
    }

    public void DesativarCamera()
    {
        CameraAtiva = false;
        LeituraPausada = true;
    }

    private void LerNovamente()
    {
        _processando = false;
        Estado = EstadoLeituraQrCode.Lendo;
        Mensagem = "Aponte a câmera para o QR Code na porta da sala.";
        Titulo = string.Empty;
        Detalhe = string.Empty;
        CameraAtiva = true;
        LeituraPausada = false;
    }

    private static bool TokenTemFormatoJwt(string? conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            return false;

        var token = conteudo.Trim();
        if (token.Length < TamanhoMinimoToken || token.Length > TamanhoMaximoToken)
            return false;

        var partes = token.Split('.');
        return partes.Length == 3 && partes.All(ParteJwtValida);
    }

    private static bool ParteJwtValida(string parte) =>
        parte.Length > 0 && parte.All(caractere =>
            char.IsAsciiLetterOrDigit(caractere) || caractere is '-' or '_');
}