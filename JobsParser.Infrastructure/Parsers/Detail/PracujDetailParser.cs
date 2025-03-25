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

            var description = string.Join(", ", ParseList(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.textSections[?(@.sectionType == 'responsibilities')].textElements[*]")) + "\n\n" + string.Join(", ", ParseList(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.textSections[?(@.sectionType == 'requirements-expected')].textElements[*]"));
            var employer = new Employer()
            {
                Name = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.displayEmployerName")?.ToString(),
            };

            return new OfferDto
            {
                Url = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.offerAbsoluteUrl")?.ToString(),
                Title = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.jobTitle")?.ToString(),
                Description = description,
                Location = jObject.SelectToken("$.props.pageProps.dehydratedState.queries[0].state.data.attributes.workplaces[0].displayAddress")?.ToString(),
                Employer = employer,

                Technologies = ParseTechnologies(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.secondaryAttributes[?(@.code == 'it-technologies-highlighted')].model.items[*].name"),
                WorkMode = ParseWorkMode(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.attributes.employment.workModes[*].pracujPlName"),
                PositionLevel = ParsePositionLevel(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.attributes.employment.positionLevels[*].pracujPlName"),
                ContractDetails = ParseContractDetails(jObject, "$.props.pageProps.dehydratedState.queries[0].state.data.attributes.employment.typesOfContract[*]")
            };
        }

        private static WorkMode ParseWorkMode(JObject jObject, string jsonPath)
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
            return results.Any() ? results[0] : null;
        }

        private static PositionLevel ParsePositionLevel(JObject jObject, string jsonPath)
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
            return results.Any() ? results[0] : null;
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

        private static ContractDetails ParseContractDetails(JObject jObject, string jsonPath)
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
            return results.Any() ? results[0] : null;
        }
        #endregion
    }
}
