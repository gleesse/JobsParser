using JobsParser.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace JobsParser.Infrastructure.Http
{
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpClientWrapper> _logger;
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(IHttpClientFactory httpClientFactory, ILogger<HttpClientWrapper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpClient = _httpClientFactory.CreateClient("default");
        }

        public void Configure(string userAgent = null, string cookieHeader = null)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrEmpty(userAgent))
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                _logger.LogInformation($"Configured User-Agent: {userAgent}");
            }

            if (!string.IsNullOrEmpty(cookieHeader))
            {
                _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
                _logger.LogInformation("Added Cookie header.");
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"GET {url}");
            var result = await _httpClient.GetAsync(url, cancellationToken);
            result.EnsureSuccessStatusCode();

            return result;
        }
    }
}
