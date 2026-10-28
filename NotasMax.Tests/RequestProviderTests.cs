using System.Net;
using System.Net.Http.Headers;
using System.Text;
using NotasMax.Exceptions;
using NotasMax.Services.RequestProvider;
using Xunit;

namespace NotasMax.Tests;

public class RequestProviderTests
{
    [Fact]
    public async Task GetAsyncDeserializesAResponseAndAddsBearerToken()
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"Value\":\"read\"}"));
        using var client = new HttpClient(handler);
        var provider = new RequestProvider(() => client);

        var result = await provider.GetAsync<TestResponse>("https://unit.test/read", "synthetic-token");

        Assert.Equal("read", result.Value);
        Assert.Equal(HttpMethod.Get, handler.LastMethod);
        Assert.Equal("https://unit.test/read", handler.LastUri);
        Assert.Equal("Bearer synthetic-token", handler.LastAuthorization);
    }

    [Fact]
    public async Task GetAsyncWithoutTokenLeavesAuthorizationUnset()
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"Value\":\"anonymous\"}"));
        using var client = new HttpClient(handler);
        var provider = new RequestProvider(() => client);

        var result = await provider.GetAsync<TestResponse>("https://unit.test/read");

        Assert.Equal("anonymous", result.Value);
        Assert.Null(handler.LastAuthorization);
    }

    [Fact]
    public async Task GetAsyncConvertsNonSuccessStatusToApiException()
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Forbidden, "{}"));
        using var client = new HttpClient(handler);
        var provider = new RequestProvider(() => client);

        var error = await Assert.ThrowsAsync<ApiException>(() => provider.GetAsync<TestResponse>("https://unit.test/denied"));

        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
    }

    [Fact]
    public async Task PostAsyncSerializesThePayloadAndDeserializesTheResponse()
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"Value\":\"saved\"}"));
        using var client = new HttpClient(handler);
        var provider = new RequestProvider(() => client);
        var payload = new TestPayload { Name = "synthetic" };

        var result = await provider.PostAsync<TestResponse, TestPayload>("https://unit.test/write", payload, "synthetic-token");

        Assert.Equal("saved", result.Value);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("application/json", handler.LastContentType?.MediaType);
        Assert.Equal("{\"Name\":\"synthetic\"}", handler.LastBody);
        Assert.Equal("Bearer synthetic-token", handler.LastAuthorization);
    }

    [Fact]
    public async Task PostAsyncThrowsApiExceptionWhenAnErrorBodyIsNotJson()
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadRequest, "not-json"));
        using var client = new HttpClient(handler);
        var provider = new RequestProvider(() => client);

        var error = await Assert.ThrowsAsync<ApiException>(() => provider.PostAsync<TestResponse, TestPayload>("https://unit.test/write", new TestPayload()));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task PostAsyncRethrowsJsonExceptionForMalformedSuccessBody()
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "not-json"));
        using var client = new HttpClient(handler);
        var provider = new RequestProvider(() => client);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => provider.PostAsync<TestResponse, TestPayload>("https://unit.test/write", new TestPayload()));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public sealed class TestResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class TestPayload
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }
        public string? LastUri { get; private set; }
        public string? LastAuthorization { get; private set; }
        public MediaTypeHeaderValue? LastContentType { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastUri = request.RequestUri?.ToString();
            LastAuthorization = request.Headers.Authorization?.ToString();
            LastContentType = request.Content?.Headers.ContentType;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }
}
