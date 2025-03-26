using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public class JsonDetailParser : IOfferDetailParser
    {
        private readonly IHttpClientWrapper _client;
        private readonly ILogger<JsonDetailParser> _logger;
        private readonly DetailParserOptions _options;

        public JsonDetailParser(IHttpClientWrapper client, ILogger<JsonDetailParser> logger, DetailParserOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<OfferDto> ParseAsync(Uri offerUrl)
        {
            try
            {
                _logger.LogInformation($"Parsing job offer from URL: {offerUrl}");

                // Get the HTML content
                var response = await _client.GetAsync(offerUrl.ToString());
                string html = await response.Content.ReadAsStringAsync();

                // Extract JSON from HTML
                string jsonString = ParserHelper.ExtractJsonFromHtml(html, _options.JsonScriptSelector);

                // Parse the JSON
                return ParseOfferFromJson(jsonString, offerUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing job offer from {offerUrl}");
                throw;
            }
        }

        private OfferDto ParseOfferFromJson(string jsonString, Uri offerUrl)
        {
            try
            {
                JObject jsonObject = JObject.Parse(jsonString);

                var offer = new OfferDto
                {
                    Url = offerUrl.ToString()
                };

                // Parse basic properties
                offer.Title = ParserHelper.GetValueFromJsonPath(jsonObject, _options.TitleSelector);
                offer.Description = ParserHelper.GetValueFromJsonPath(jsonObject, _options.DescriptionSelector);
                offer.Location = ParserHelper.GetValueFromJsonPath(jsonObject, _options.LocationSelector);

                // Parse related entities
                offer.Employer = ParserHelper.CreateEmployer(
                    ParserHelper.GetValueFromJsonPath(jsonObject, _options.EmployerNameSelector)
                );

                offer.WorkMode = ParserHelper.CreateWorkMode(
                    ParserHelper.GetValueFromJsonPath(jsonObject, _options.WorkModeSelector)
                );

                offer.PositionLevel = ParserHelper.CreatePositionLevel(
                    ParserHelper.GetValueFromJsonPath(jsonObject, _options.PositionLevelSelector)
                );

                // Parse technologies
                var technologies = ParserHelper.GetArrayFromJsonPath(jsonObject, _options.TechnologiesSelector);
                offer.Technologies = ParserHelper.CreateTechnologies(technologies);

                // Parse contract details
                var contractType = ParserHelper.GetValueFromJsonPath(jsonObject, _options.ContractTypeSelector);

                decimal? minSalary = ParserHelper.TryParseDecimal(
                    ParserHelper.GetValueFromJsonPath(jsonObject, _options.MinSalarySelector)
                );

                decimal? maxSalary = ParserHelper.TryParseDecimal(
                    ParserHelper.GetValueFromJsonPath(jsonObject, _options.MaxSalarySelector)
                );

                var currency = ParserHelper.GetValueFromJsonPath(jsonObject, _options.CurrencySelector);
                var timeUnit = ParserHelper.GetValueFromJsonPath(jsonObject, _options.TimeUnitSelector);

                offer.ContractDetails = ParserHelper.CreateContractDetails(
                    contractType, minSalary, maxSalary, currency, timeUnit
                );

                return offer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON content");
                throw;
            }
        }
    }
}
