using BarcodeScanning;
using NotasMax.ViewModels;

namespace NotasMax.Views.Aluno;

public partial class LerQrCodeView : ContentPage
{
    private readonly LerQrCodeViewModel _viewModel;

    public LerQrCodeView(LerQrCodeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.IniciarSolicitacaoPermissao();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (status == PermissionStatus.Granted)
                _viewModel.PermissaoConcedida();
            else
                _viewModel.PermissaoNegada();
        });
    }

    protected override void OnDisappearing()
    {
        QrCamera.PauseScanning = true;
        QrCamera.CameraEnabled = false;
        _viewModel.DesativarCamera();
        base.OnDisappearing();
    }

    private async void Camera_OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        if (e.BarcodeResults.Count == 0 || _viewModel.Estado != EstadoLeituraQrCode.Lendo)
            return;

        var conteudo = e.BarcodeResults.First().RawValue;
        if (string.IsNullOrWhiteSpace(conteudo))
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            QrCamera.PauseScanning = true;
            QrCamera.CameraEnabled = false;
            await _viewModel.ProcessarLeituraAsync(conteudo);
        });
    }
}