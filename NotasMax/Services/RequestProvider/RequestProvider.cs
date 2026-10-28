using NotasMax.Exceptions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NotasMax.Services.RequestProvider
{
    public class RequestProvider : IRequestProvider
    {

        private readonly Lazy<HttpClient> _httpClient;

        public RequestProvider() : this(() => new HttpClient())
        {
        }

        internal RequestProvider(Func<HttpClient> httpClientFactory)
        {
            _httpClient = new Lazy<HttpClient>(() =>
            {
                var httpClient = httpClientFactory();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                return httpClient;
            });
        }

        // Metodo auxiliador para obter ou criar o HttpClient com token de autenticação
        private HttpClient GetOrCreateHttpClient(string token)
        {
            var httpClient = _httpClient.Value;
            httpClient.DefaultRequestHeaders.Clear(); // Limpa o cabeçalho para evitar acúmulo de tokens

            // Adiciona o token apenas se não for nulo ou vazio
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return httpClient;
        }

        // Métodos para realizar requisições HTTP (GET, POST, PUT, DELETE) usando o HttpClient

        // Método POST - Dinâmico para enviar dados e receber uma resposta do mesmo tipo
        public async Task<TResult> PostAsync<TResult, TSend>(string uri, TSend data, string token = "")
        {
            var httpClient = GetOrCreateHttpClient(token);

            // Serializa o objeto de dados "data" para JSON e cria um StringContent para o corpo da requisição
            var body = new StringContent(JsonSerializer.Serialize(data));
            body.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // Define o tipo de conteúdo como JSON

            // Envia a requisição POST para a URI especificada e aguarda a resposta
            var response = await httpClient.PostAsync(uri, body).ConfigureAwait(false);

            // Lê o conteúdo da resposta
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Tenta desserializar a resposta mesmo se não for sucesso (pode conter mensagem de erro)
            try
            {
                var result = JsonSerializer.Deserialize<TResult>(content);
                return result;
            }
            catch (JsonException)
            {
                // Se não conseguir desserializar, verifica se foi sucesso
                if (!response.IsSuccessStatusCode)
                    throw new ApiException(response.StatusCode, "Erro ao realizar a requisição com a API");

                // Se foi sucesso mas não conseguiu desserializar, relança o erro de JSON
                throw;
            }
        }
        
        public async Task<TResult> GetAsync<TResult>(string uri, string token = "")
        {
            var httpClient = GetOrCreateHttpClient(token);
            
            var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
                throw new ApiException(response.StatusCode, "Erro ao realizar a requisição com a API");


            var result = await response.Content.ReadFromJsonAsync<TResult>();
            return result;
        }
    }
}
