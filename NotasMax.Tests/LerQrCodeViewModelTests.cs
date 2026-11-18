using NotasMax.Models.Presencas;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using Xunit;

namespace NotasMax.Tests;

public class LerQrCodeViewModelTests
{
    private const string JwtValido = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature";

    [Fact]
    public async Task ProcessarLeituraChamaServicoUmaUnicaVezParaDeteccoesRepetidas()
    {
        var presenca = new FakePresencaService
        {
            Resultado = new ResultadoPresenca { Status = StatusPresenca.Sucesso }
        };
        var vm = CriarVm(presenca);
        vm.PermissaoConcedida();

        await vm.ProcessarLeituraAsync(JwtValido);
        await vm.ProcessarLeituraAsync(JwtValido);

        Assert.Equal(1, presenca.CallCount);
        Assert.Equal(EstadoLeituraQrCode.Sucesso, vm.Estado);
    }

    [Theory]
    [InlineData(StatusPresenca.Sucesso, EstadoLeituraQrCode.Sucesso)]
    [InlineData(StatusPresenca.JaRegistrada, EstadoLeituraQrCode.JaRegistrada)]
    [InlineData(StatusPresenca.TurmaDiferente, EstadoLeituraQrCode.TurmaDiferente)]
    [InlineData(StatusPresenca.QrInvalido, EstadoLeituraQrCode.QrInvalido)]
    [InlineData(StatusPresenca.SemConexao, EstadoLeituraQrCode.SemConexao)]
    [InlineData(StatusPresenca.ErroServidor, EstadoLeituraQrCode.ErroServidor)]
    public async Task ProcessarLeituraTransitaParaEstadoDoResultado(StatusPresenca status, EstadoLeituraQrCode estadoEsperado)
    {
        var presenca = new FakePresencaService
        {
            Resultado = new ResultadoPresenca
            {
                Status = status,
                Mensagem = "mensagem",
                Simulado = new SimuladoPresencaResponse { Numero = 4, Tipo = "ENEM" }
            }
        };
        var vm = CriarVm(presenca);
        vm.PermissaoConcedida();

        await vm.ProcessarLeituraAsync(JwtValido);

        Assert.Equal(estadoEsperado, vm.Estado);
        if (status == StatusPresenca.JaRegistrada)
            Assert.False(vm.CameraAtiva);
    }

    [Fact]
    public async Task ProcessarLeituraNaoChamaServicoParaConteudoInvalido()
    {
        var presenca = new FakePresencaService();
        var vm = CriarVm(presenca);
        vm.PermissaoConcedida();

        await vm.ProcessarLeituraAsync("https://exemplo.test");

        Assert.Equal(0, presenca.CallCount);
        Assert.Equal(EstadoLeituraQrCode.QrInvalido, vm.Estado);
    }

    [Fact]
    public async Task SessaoExpiradaNavegaParaLogin()
    {
        var navigation = new FakeNavigationService();
        var presenca = new FakePresencaService
        {
            Resultado = new ResultadoPresenca { Status = StatusPresenca.SessaoExpirada }
        };
        var vm = new LerQrCodeViewModel(presenca, navigation);
        vm.PermissaoConcedida();

        await vm.ProcessarLeituraAsync(JwtValido);

        Assert.Contains(navigation.Navigations,
            item => item.Route == "Login?Logout=true" && item.Absolute);
    }

    private static LerQrCodeViewModel CriarVm(FakePresencaService presenca) =>
        new(presenca, new FakeNavigationService());
}
