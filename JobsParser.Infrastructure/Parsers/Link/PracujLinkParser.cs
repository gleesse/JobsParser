using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JobsParser.Infrastructure.Parsers.Link
{
    public class PracujLinkParser : IOfferLinkParser
    {
        private const string DATA_XPATH = "//script[@id='__NEXT_DATA__']";
        private readonly IHttpClientWrapper _client;
        private readonly ILogger<PracujLinkParser> _logger;

        public PracujLinkParser(IHttpClientWrapper client, ILogger<PracujLinkParser> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<IEnumerable<OfferLinkDto>> ParseAsync(Uri searchUrl, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(searchUrl);

            _logger.LogInformation($"Parsing offer links from Pracuj.pl at URL: {searchUrl}");

            try
            {
                var offerUrls = new List<OfferLinkDto>();
                var cleanJson = await GetCleanJsonAsync(searchUrl, cancellationToken);
                if (string.IsNullOrEmpty(cleanJson))
                {
                    _logger.LogWarning($"No JSON data found at {searchUrl}. Aborting parse.");
                    return Enumerable.Empty<OfferLinkDto>();
                }

                JsonNode rootNode = JsonNode.Parse(cleanJson);
                if (rootNode == null)
                {
                    _logger.LogError($"Failed to parse JSON from {searchUrl}.");
                    return Enumerable.Empty<OfferLinkDto>();
                }

                offerUrls = ExtractOfferUrls(rootNode);
                _logger.LogInformation($"Successfully extracted {offerUrls.Count} offer links from {searchUrl}.");
                return offerUrls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing offer links from {searchUrl}.");
                throw;
            }
        }

        #region Helper methods
        private async Task<string> GetCleanJsonAsync(Uri url, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching HTML content from {url}...");
            string html = null;
            try
            {
                var response = await _client.GetAsync(url.ToString(), cancellationToken);
                response.EnsureSuccessStatusCode();
                html = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request to {url} failed.");
                throw;
            }

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning($"No HTML content received from {url}.");
                return null;
            }

            HtmlNode scriptNode = null;
            try
            {
                scriptNode = GetScriptNode(html, DATA_XPATH);
            }
            catch (NodeNotFoundException ex)
            {
                _logger.LogError(ex, $"Script node with XPath '{DATA_XPATH}' not found in HTML from {url}.");
                return null;
            }

            if (scriptNode == null)
            {
                _logger.LogError($"GetScriptNode returned null for URL: {url}");
                return null; // Or throw, depending on how you want to handle missing script node
            }

            try
            {
                string cleanJson = ExtractJson(scriptNode.InnerText);
                _logger.LogDebug($"Successfully extracted JSON from script node for URL: {url}");
                return cleanJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting JSON from script node for URL: {url}.");
                return null;
            }
        }

        private HtmlNode GetScriptNode(string html, string xpath)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentException("HTML cannot be null or empty.", nameof(html));
            }

            HtmlDocument doc = new();
            doc.LoadHtml(html);
            HtmlNode scriptNode = doc.DocumentNode.SelectSingleNode(xpath);

            if (scriptNode == null)
            {
                _logger.LogError($"Script node with XPath '{xpath}' not found.");
                throw new NodeNotFoundException($"Script node with XPath '{xpath}' not found.");
            }

            return scriptNode;
        }

        private string ExtractJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                _logger.LogWarning("JSON string is null or empty.");
                return null;
            }

            try
            {
                int startIndex = jsonString.IndexOf("{", StringComparison.Ordinal);
                int endIndex = jsonString.LastIndexOf("}", StringComparison.Ordinal) + 1;

                if (startIndex < 0 || endIndex <= startIndex)
                {
                    _logger.LogWarning($"Could not find valid JSON bounds in string: {jsonString}.");
                    return null;
                }

                return jsonString.Substring(startIndex, endIndex - startIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting JSON substring.");
                return null;
            }
        }

        private List<OfferLinkDto> ExtractOfferUrls(JsonNode rootNode)
        {
            if (rootNode == null)
            {
                _logger.LogWarning("Root node is null. Cannot extract offer URLs.");
                return new List<OfferLinkDto>();
            }

            try
            {
                var groupedOffersNode = rootNode["props"]?["pageProps"]?["data"]?["jobOffers"]?["groupedOffers"] as JsonNode;

                if (groupedOffersNode is null || groupedOffersNode.GetValueKind() != JsonValueKind.Array)
                {
                    _logger.LogWarning("groupedOffersNode is null or not an array. Cannot extract offer URLs.");
                    return new List<OfferLinkDto>();
                }

                var offerUrls = groupedOffersNode.AsArray()
                    .SelectMany(groupedOffer => (groupedOffer?["offers"] as JsonNode)?.AsArray() ?? new JsonArray())
                    .Select(offer => offer?["offerAbsoluteUri"]?.ToString())
                    .Where(url => !string.IsNullOrEmpty(url))
                    .Select(url => new OfferLinkDto { SourceUrl = new Uri(url) })
                    .ToList();

                return offerUrls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting offer URLs from JSON.");
                return new List<OfferLinkDto>();
            }
        }
        #endregion
    }
}
