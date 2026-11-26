using System.Net;
using NotasMax.Models.Presencas;
using NotasMax.Services.Presencas;
using NotasMax.Services.RequestProvider;
using NotasMax.Tests.TestSupport;
using Xunit;

namespace NotasMax.Tests;

public class PresencaServiceTests
{
    [Fact]
    public async Task RegistrarPresencaMapsCreated()
    {
        var provider = new RequestProviderStub
        {
            StatusCode = HttpStatusCode.Created,
            Response = new RegistrarPresencaResponse
            {
                Message = "Presença registrada!",
                Simulado = new SimuladoPresencaResponse { Id = "1", Numero = 3, Tipo = "ENEM" }
            }
        };

        var result = await new PresencaService(provider, new FakeSettingsService()).RegistrarPresenca("header.payload.signature");

        Assert.Equal(StatusPresenca.Sucesso, result.Status);
        Assert.Equal(HttpStatusCode.Created, result.StatusHttp);
        Assert.Equal(3, result.Simulado?.Numero);
        Assert.Equal("synthetic-token", provider.LastToken);
        Assert.Equal("header.payload.signature", Assert.IsType<RegistrarPresencaRequest>(provider.LastPayload).Token);
    }

    [Fact]
    public async Task RegistrarPresencaMapsConflict()
    {
        var result = await Registrar(HttpStatusCode.Conflict, "já registrada");
        Assert.Equal(StatusPresenca.JaRegistrada, result.Status);
        Assert.Equal("Você já registrou presença neste simulado.", result.Mensagem);
    }

    [Fact]
    public async Task RegistrarPresencaMapsForbiddenTurma()
    {
        var result = await Registrar(HttpStatusCode.Forbidden, "Aluno não pertence à turma do simulado");
        Assert.Equal(StatusPresenca.TurmaDiferente, result.Status);
    }

    [Fact]
    public async Task RegistrarPresencaMapsForbiddenSession()
    {
        var result = await Registrar(HttpStatusCode.Forbidden, "Sessão inválida ou expirada");
        Assert.Equal(StatusPresenca.SessaoExpirada, result.Status);
    }

    [Fact]
    public async Task RegistrarPresencaMapsUnauthorized()
    {
        var result = await Registrar(HttpStatusCode.Unauthorized, "Não autorizado");
        Assert.Equal(StatusPresenca.SessaoExpirada, result.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task RegistrarPresencaMapsInvalidQrStatuses(HttpStatusCode status)
    {
        var result = await Registrar(status, "QR expirado ou revogado");
        Assert.Equal(StatusPresenca.QrInvalido, result.Status);
        Assert.Equal("QR expirado ou revogado", result.Mensagem);
    }

    [Fact]
    public async Task RegistrarPresencaMapsServerError()
    {
        var result = await Registrar(HttpStatusCode.InternalServerError, "falha");
        Assert.Equal(StatusPresenca.ErroServidor, result.Status);
    }

    [Fact]
    public async Task RegistrarPresencaMapsHttpRequestException()
    {
        var provider = new RequestProviderStub { ExceptionToThrow = new HttpRequestException("offline") };
        var result = await new PresencaService(provider, new FakeSettingsService()).RegistrarPresenca("token");
        Assert.Equal(StatusPresenca.SemConexao, result.Status);
    }

    [Fact]
    public async Task RegistrarPresencaMapsTimeout()
    {
        var provider = new RequestProviderStub { ExceptionToThrow = new TaskCanceledException("timeout") };
        var result = await new PresencaService(provider, new FakeSettingsService()).RegistrarPresenca("token");
        Assert.Equal(StatusPresenca.SemConexao, result.Status);
    }

    [Fact]
    public async Task RegistrarPresencaDoesNotCallProviderWithoutSession()
    {
        var provider = new RequestProviderStub { StatusCode = HttpStatusCode.Created };
        var settings = new FakeSettingsService { AuthAccessToken = string.Empty };

        var result = await new PresencaService(provider, settings).RegistrarPresenca("token");

        Assert.Equal(StatusPresenca.SessaoExpirada, result.Status);
        Assert.Null(provider.LastUri);
    }

    private static Task<ResultadoPresenca> Registrar(HttpStatusCode status, string message)
    {
        var provider = new RequestProviderStub
        {
            StatusCode = status,
            Response = new RegistrarPresencaResponse { Message = message }
        };
        return new PresencaService(provider, new FakeSettingsService()).RegistrarPresenca("header.payload.signature");
    }

    private sealed class RequestProviderStub : IRequestProvider
    {
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
        public object? Response { get; init; }
        public Exception? ExceptionToThrow { get; init; }
        public string? LastUri { get; private set; }
        public string? LastToken { get; private set; }
        public object? LastPayload { get; private set; }

        public Task<(HttpStatusCode StatusCode, TResult? Data)> PostWithStatusAsync<TResult, TSend>(string uri, TSend data, string token = "")
        {
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            LastUri = uri;
            LastToken = token;
            LastPayload = data;
            return Task.FromResult((StatusCode, (TResult?)Response));
        }

        public Task<TResult> PostAsync<TResult, TSend>(string uri, TSend data, string token = "") => throw new NotImplementedException();
        public Task<TResult> GetAsync<TResult>(string uri, string token = "") => throw new NotImplementedException();
    }
}