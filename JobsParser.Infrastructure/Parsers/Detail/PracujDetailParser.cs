using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public class PracujDetailParser : IOfferDetailParser
    {
        private const string DATA_XPATH = "//script[@id='__NEXT_DATA__']";
        private readonly IHttpClientWrapper _client;
        private readonly ILogger<PracujDetailParser> _logger;

        public PracujDetailParser(IHttpClientWrapper client, ILogger<PracujDetailParser> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<OfferDto> ParseAsync(Uri offerUrl, CancellationToken cancellationToken = default)
        {
            var cleanJson = await GetCleanJsonAsync(offerUrl);
            return CreateJobOffer(cleanJson);
        }

        #region Helper methods
        private async Task<string> GetCleanJsonAsync(Uri url)
        {
            var response = await _client.GetAsync(url.ToString());
            string html = await response.Content.ReadAsStringAsync();
            HtmlNode scriptNode = GetScriptNode(html, DATA_XPATH);
            string cleanJson = ExtractJson(scriptNode.InnerText);

            return cleanJson;
        }
        private HtmlNode GetScriptNode(string html, string xpath)
        {
            HtmlDocument doc = new();
            doc.LoadHtml(html);
            HtmlNode scriptNode = doc.DocumentNode.SelectSingleNode(xpath);

            if (scriptNode == null)
            {
                throw new NodeNotFoundException("Script node not found.");
            }

            return scriptNode;
        }

        private string ExtractJson(string jsonString)
        {
            int startIndex = jsonString.IndexOf("{");
            int endIndex = jsonString.LastIndexOf("}") + 1;
            return jsonString.Substring(startIndex, endIndex - startIndex);
        }

        private OfferDto CreateJobOffer(string json)
        {
            JObject jObject = JObject.Parse(json);

            return new OfferDto
            {
                OfferUrl = new Uri(jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.offerAbsoluteUrl")?.ToString()),
                ApplicationUrl = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.applying.applyUrl")?.ToString(),
                IsActive = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.publicationDetails.isActive")?.ToObject<bool?>(),
                OneClickApply = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.applying.oneClickApply")?.ToObject<bool?>(),
                CreatedAt = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.publicationDetails.dateOfInitialPublicationUtc")?.ToObject<DateTime?>(),
                UpdatedAt = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.publicationDetails.lastPublishedUtc")?.ToObject<DateTime?>(),
                ValidUntil = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.publicationDetails.expirationDateTimeUtc")?.ToObject<DateTime?>(),
                SourceOfferId = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.jobOfferWebId")?.ToObject<int?>(),
                Title = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.jobTitle")?.ToString(),
                Description = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.description")?.ToString(),
                EmployerId = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.employerId")?.ToObject<int?>(),
                Employer = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.displayEmployerName")?.ToString(),
                Location = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.workplaces[0].displayAddress")?.ToString(),
                Technologies = ParseTechnologies(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.secondaryAttributes[?(@.code == 'it-technologies-highlighted')].model.items[*].name"),
                Responsibilities = string.Join(", ", ParseList(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.textSections[?(@.sectionType == 'responsibilities')].textElements[*]")),
                Requirements = string.Join(", ", ParseList(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.textSections[?(@.sectionType == 'requirements-expected')].textElements[*]")),
                WorkModes = ParseWorkModes(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.attributes.employment.workModes[*].pracujPlName"),
                PositionLevels = ParsePositionLevels(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.attributes.employment.positionLevels[*].pracujPlName"),
                AboutUs = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.textSections[?(@.sectionType == 'about-us-description')].textElements[0]")?.ToString(),
                ContractDetails = ParseContractDetails(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.attributes.employment.typesOfContract[*]")
            };
        }

        private static List<WorkMode> ParseWorkModes(JObject jObject, string jsonPath)
        {
            var tokens = jObject.SelectTokens(jsonPath);
            List<WorkMode> results = new();
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    if (token != null && !string.IsNullOrEmpty(token.ToString()))
                    {
                        results.Add(new WorkMode { Name = token.ToString() });
                    }
                }
            }
            return results;
        }

        private static List<PositionLevel> ParsePositionLevels(JObject jObject, string jsonPath)
        {
            var tokens = jObject.SelectTokens(jsonPath);
            List<PositionLevel> results = new();
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    if (token != null && !string.IsNullOrEmpty(token.ToString()))
                    {
                        results.Add(new PositionLevel { Name = token.ToString() });
                    }
                }
            }
            return results;
        }

        private static List<Technology> ParseTechnologies(JObject jObject, string jsonPath)
        {
            var tokens = jObject.SelectTokens(jsonPath);
            List<Technology> results = new();
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    if (token != null && !string.IsNullOrEmpty(token.ToString()))
                    {
                        results.Add(new Technology { Name = token.ToString() });
                    }
                }
            }
            return results;
        }

        private static List<string> ParseList(JObject jObject, string jsonPath)
        {
            var tokens = jObject.SelectTokens(jsonPath);
            List<string> results = new();
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    if (token != null && !string.IsNullOrEmpty(token.ToString()))
                    {
                        results.Add(token.ToString());
                    }
                }
            }
            return results;
        }

        private static List<ContractDetails> ParseContractDetails(JObject jObject, string jsonPath)
        {
            var tokens = jObject.SelectTokens(jsonPath);
            List<ContractDetails> results = new();
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    if (token != null)
                    {
                        var contractDetails = new ContractDetails()
                        {
                            TypeOfContract = token.SelectToken("$.pracujPlName")?.ToString(),
                            MinSalary = decimal.Parse(token.SelectToken("$.salary.from")?.ToString()),
                            MaxSalary = decimal.Parse(token.SelectToken("$.salary.to")?.ToString()),
                            Currency = token.SelectToken("$.salary.currency.code")?.ToString(),
                            TimeUnit = token.SelectToken("$.salary.timeUnit.longForm.name")?.ToString(),
                        };
                        results.Add(contractDetails);
                    }
                }
            }
            return results;
        }
        #endregion
    }
}
