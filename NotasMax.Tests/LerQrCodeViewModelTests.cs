using System.Net;
using NotasMax.Models.Presencas;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using Xunit;

namespace NotasMax.Tests;

public class LerQrCodeViewModelTests
{
    private const string TokenValido =
        "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature";

    private static LerQrCodeViewModel CriarViewModel(FakePresencaService service) =>
        new(service, new FakeNavigationService());

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoEstadoNaoForLendo_NaoDeveChamarServico()
    {
        var service = new FakePresencaService();
        var viewModel = CriarViewModel(service);

        await viewModel.ProcessarLeituraAsync(TokenValido);

        Assert.Equal(0, service.CallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("abc.def")]
    [InlineData("abc..def")]
    [InlineData(".abc.def")]
    [InlineData("abc.def.")]
    [InlineData("abc.def.invalid!")]
    [InlineData("a.b.c")]
    public async Task ProcessarLeituraAsync_QuandoTokenForInvalido_NaoDeveChamarServico(string token)
    {
        var service = new FakePresencaService();
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(token);

        Assert.Equal(0, service.CallCount);
        Assert.Equal(EstadoLeituraQrCode.QrInvalido, viewModel.Estado);
        Assert.False(viewModel.IsProcessando);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoTokenForNulo_NaoDeveChamarServico()
    {
        var service = new FakePresencaService();
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(null!);

        Assert.Equal(0, service.CallCount);
        Assert.Equal(EstadoLeituraQrCode.QrInvalido, viewModel.Estado);
        Assert.False(viewModel.IsProcessando);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoTokenExcederLimite_NaoDeveChamarServico()
    {
        var service = new FakePresencaService();
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(new string('a', 8193));

        Assert.Equal(0, service.CallCount);
        Assert.Equal(EstadoLeituraQrCode.QrInvalido, viewModel.Estado);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoTokenForValido_DeveEnviarTokenSemEspacosExternos()
    {
        var service = new FakePresencaService();
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync($"  {TokenValido}  ");

        Assert.Equal(1, service.CallCount);
        Assert.Equal(TokenValido, Assert.Single(service.TokensRecebidos));
        Assert.Equal(EstadoLeituraQrCode.Sucesso, viewModel.Estado);
        Assert.False(viewModel.IsProcessando);
    }

    [Theory]
    [InlineData(StatusPresenca.Sucesso, EstadoLeituraQrCode.Sucesso)]
    [InlineData(StatusPresenca.JaRegistrada, EstadoLeituraQrCode.JaRegistrada)]
    [InlineData(StatusPresenca.TurmaDiferente, EstadoLeituraQrCode.TurmaDiferente)]
    [InlineData(StatusPresenca.QrInvalido, EstadoLeituraQrCode.QrInvalido)]
    [InlineData(StatusPresenca.SemConexao, EstadoLeituraQrCode.SemConexao)]
    [InlineData(StatusPresenca.ErroServidor, EstadoLeituraQrCode.ErroServidor)]
    public async Task ProcessarLeituraAsync_DeveMapearStatusDoServico(
        StatusPresenca status, EstadoLeituraQrCode estadoEsperado)
    {
        var service = new FakePresencaService
        {
            Resultado = new ResultadoPresenca
            {
                Status = status,
                Mensagem = "Mensagem de teste"
            }
        };
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(TokenValido);

        Assert.Equal(estadoEsperado, viewModel.Estado);
        Assert.Equal(1, service.CallCount);
        Assert.False(viewModel.IsProcessando);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoServicoLancarExcecao_DeveExibirErroServidorELiberarProcessamento()
    {
        var service = new FakePresencaService
        {
            ExceptionToThrow = new HttpRequestException("Falha simulada")
        };
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(TokenValido);

        Assert.Equal(1, service.CallCount);
        Assert.Equal(EstadoLeituraQrCode.ErroServidor, viewModel.Estado);
        Assert.False(viewModel.IsProcessando);
        Assert.False(viewModel.IsCameraVisivel);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoServicoRetornarNulo_DeveTratarComoErroServidor()
    {
        var service = new FakePresencaService { Resultado = null! };
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(TokenValido);

        Assert.Equal(EstadoLeituraQrCode.ErroServidor, viewModel.Estado);
        Assert.False(viewModel.IsProcessando);
    }

    [Fact]
    public void EstadosDePermissao_EstadosDerivadosDevemRefletirCamera()
    {
        var viewModel = CriarViewModel(new FakePresencaService());

        viewModel.PermissaoNegada();
        Assert.True(viewModel.IsPermissaoNegada);
        Assert.False(viewModel.CameraAtiva);
        Assert.True(viewModel.LeituraPausada);

        viewModel.IniciarSolicitacaoPermissao();
        Assert.True(viewModel.IsSolicitandoPermissao);
        Assert.False(viewModel.CameraAtiva);

        viewModel.PermissaoConcedida();
        Assert.True(viewModel.IsCameraVisivel);
        Assert.True(viewModel.CameraAtiva);
        Assert.False(viewModel.LeituraPausada);

        viewModel.DesativarCamera();
        Assert.False(viewModel.CameraAtiva);
        Assert.True(viewModel.LeituraPausada);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoSessaoExpirar_DeveIrParaLogin()
    {
        var service = new FakePresencaService
        {
            Resultado = new ResultadoPresenca { Status = StatusPresenca.SessaoExpirada }
        };
        var navigation = new FakeNavigationService();
        var viewModel = new LerQrCodeViewModel(service, navigation);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(TokenValido);

        Assert.Single(navigation.Navigations);
        Assert.Equal("Login?Logout=true", navigation.Navigations[0].Route);
        Assert.True(navigation.Navigations[0].Absolute);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_QuandoHaSucessoComSimulado_DeveExibirDetalhes()
    {
        var service = new FakePresencaService
        {
            Resultado = new ResultadoPresenca
            {
                Status = StatusPresenca.Sucesso,
                Mensagem = "Presença registrada!",
                Simulado = new SimuladoPresencaResponse { Numero = 7, Tipo = "ENEM" }
            }
        };
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(TokenValido);

        Assert.True(viewModel.IsSucesso);
        Assert.Equal("Simulado 7 • ENEM", viewModel.Mensagem);
        Assert.False(viewModel.IsCameraVisivel);
    }

    [Fact]
    public void PropriedadesDerivadasDevemRefletirEstadoDeLeitura()
    {
        var viewModel = CriarViewModel(new FakePresencaService());

        viewModel.PermissaoNegada();
        Assert.True(viewModel.IsPermissaoNegada);

        viewModel.PermissaoConcedida();
        Assert.True(viewModel.IsCameraVisivel);
        Assert.False(viewModel.IsErro);
        Assert.False(viewModel.PodeLerOutroQr);
        Assert.False(viewModel.PodeTentarNovamente);

        viewModel.TentarNovamenteCommand.Execute(null);
        Assert.True(viewModel.IsCameraVisivel);
    }

    [Fact]
    public async Task LerNovamente_DeveReativarLeituraAposErro()
    {
        var service = new FakePresencaService
        {
            Resultado = new ResultadoPresenca { Status = StatusPresenca.ErroServidor }
        };
        var viewModel = CriarViewModel(service);
        viewModel.PermissaoConcedida();

        await viewModel.ProcessarLeituraAsync(TokenValido);
        viewModel.LerNovamenteCommand.Execute(null);

        Assert.Equal(EstadoLeituraQrCode.Lendo, viewModel.Estado);
        Assert.True(viewModel.IsCameraVisivel);
        Assert.False(viewModel.IsProcessando);
    }
}
