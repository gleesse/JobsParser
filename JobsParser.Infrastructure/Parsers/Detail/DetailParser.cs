using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public class DetailParser : IOfferDetailParser
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly ILogger _logger;
        private readonly DetailParserOptions _options;
        private readonly string _parserType;

        public DetailParser(IHttpClientWrapper httpClient, ILogger logger, DetailParserOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _parserType = options.Type?.ToLower() ?? throw new ArgumentNullException(nameof(options.Type));
        }

        public async Task<OfferDto> ParseAsync(Uri offerUrl)
        {
            try
            {
                _logger.LogInformation($"Parsing job offer from URL: {offerUrl} using {_parserType} parser");

                // Fetch content
                var response = await _httpClient.GetAsync(offerUrl.ToString());
                string html = await response.Content.ReadAsStringAsync();

                // Create the appropriate extractor
                IValueExtractor extractor = CreateExtractor(html);

                // Parse the offer using the extractor
                return ParseOffer(extractor, offerUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing job offer from {offerUrl}");
                throw;
            }
        }

        private IValueExtractor CreateExtractor(string content)
        {
            return _parserType switch
            {
                "json" => new JsonValueExtractor(content, _options.JsonScriptSelector),
                "html" => new HtmlValueExtractor(content),
                _ => throw new ArgumentException($"Unsupported parser type: {_parserType}")
            };
        }

        private OfferDto ParseOffer(IValueExtractor extractor, Uri sourceUrl)
        {
            try
            {
                var offer = new OfferDto
                {
                    Url = sourceUrl.ToString(),
                    Title = extractor.ExtractValue(_options.TitleSelector),
                    Responsibilities = extractor.ExtractValue(_options.ResponsibilitiesSelector),
                    Requirements = extractor.ExtractValue(_options.RequirementsSelector),
                    Location = extractor.ExtractValue(_options.LocationSelector)
                };

                var employerName = extractor.ExtractValue(_options.EmployerNameSelector);
                offer.Employer = !string.IsNullOrEmpty(employerName) ? new Employer { Name = employerName } : null;

                var workMode = extractor.ExtractValue(_options.WorkModeSelector);
                offer.WorkMode = !string.IsNullOrEmpty(workMode) ? new WorkMode { Name = workMode } : null;

                var positionLevel = extractor.ExtractValue(_options.PositionLevelSelector);
                offer.PositionLevel = !string.IsNullOrEmpty(positionLevel) ? new PositionLevel { Name = positionLevel } : null;

                // Parse technologies
                var technologies = extractor.ExtractList(_options.TechnologiesSelector);
                offer.Technologies = technologies.Count != 0 ? technologies.Select(name => new Technology { Name = name }).ToList() : [];

                // Parse contract details
                var contractType = extractor.ExtractValue(_options.ContractTypeSelector);

                decimal? minSalary = decimal.TryParse(extractor.ExtractValue(_options.MinSalarySelector), out decimal minSalaryResult) ? minSalaryResult : null;
                decimal? maxSalary = decimal.TryParse(extractor.ExtractValue(_options.MaxSalarySelector), out decimal maxSalaryResult) ? maxSalaryResult : null;

                var currency = extractor.ExtractValue(_options.CurrencySelector);
                var timeUnit = extractor.ExtractValue(_options.TimeUnitSelector);

                offer.ContractDetails = !string.IsNullOrEmpty(contractType) ? new ContractDetails
                {
                    TypeOfContract = contractType,
                    MinSalary = minSalary ?? 0,
                    MaxSalary = maxSalary ?? 0,
                    Currency = currency,
                    TimeUnit = timeUnit
                } : null;

                return offer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing content with {_parserType} parser");
                throw;
            }
        }

        #region Value Extractors

        private interface IValueExtractor
        {
            string ExtractValue(string selector);
            List<string> ExtractList(string selector);
        }

        private class HtmlValueExtractor : IValueExtractor
        {
            private readonly HtmlDocument _document;

            public HtmlValueExtractor(string htmlContent)
            {
                _document = new HtmlDocument();
                _document.LoadHtml(htmlContent);
            }

            public string ExtractValue(string selector)
            {
                return _document.ExtractText(selector);
            }

            public List<string> ExtractList(string selector)
            {
                return _document.ExtractNodeList(selector);
            }
        }

        private class JsonValueExtractor : IValueExtractor
        {
            private readonly JObject _jsonObject;

            public JsonValueExtractor(string htmlContent, string scriptSelector)
            {
                string jsonString = ExtractJsonFromHtml(htmlContent, scriptSelector);
                _jsonObject = JObject.Parse(jsonString);
            }

            public string ExtractValue(string selector)
            {
                return _jsonObject.GetValueByJsonPath(selector);
            }

            public List<string> ExtractList(string selector)
            {
                return _jsonObject.GetArrayByJsonPath(selector);
            }

            private string ExtractJsonFromHtml(string html, string scriptSelector)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var scriptNode = doc.DocumentNode.SelectSingleNode(scriptSelector);
                if (scriptNode == null)
                {
                    throw new Exception($"Script node not found using selector: {scriptSelector}");
                }

                string scriptContent = scriptNode.InnerText;

                // Extract the JSON object from the script
                int startIndex = scriptContent.IndexOf("{");
                int endIndex = scriptContent.LastIndexOf("}") + 1;

                if (startIndex < 0 || endIndex <= 0 || endIndex <= startIndex)
                {
                    throw new Exception("Valid JSON object not found in script content");
                }

                return scriptContent.Substring(startIndex, endIndex - startIndex);
            }
        }

        #endregion
    }
} 