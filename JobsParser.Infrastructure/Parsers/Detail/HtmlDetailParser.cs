using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public class HtmlDetailParser : IOfferDetailParser
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly ILogger<HtmlDetailParser> _logger;
        private readonly DetailParserOptions _options;

        public HtmlDetailParser(IHttpClientWrapper httpClient, ILogger<HtmlDetailParser> logger, DetailParserOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<OfferDto> ParseAsync(Uri offerUrl)
        {
            try
            {
                _logger.LogInformation($"Parsing job offer from URL: {offerUrl}");

                // Fetch HTML content
                var response = await _httpClient.GetAsync(offerUrl.ToString());
                string html = await response.Content.ReadAsStringAsync();

                // Parse HTML
                return ParseOfferFromHtml(html, offerUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing job offer from {offerUrl}");
                throw;
            }
        }

        private OfferDto ParseOfferFromHtml(string html, Uri sourceUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var offer = new OfferDto
            {
                Url = sourceUrl.ToString()
            };

            try
            {
                // Parse basic properties
                offer.Title = ParserHelper.ExtractText(doc, _options.TitleSelector);
                offer.Description = ParserHelper.ExtractText(doc, _options.DescriptionSelector);
                offer.Location = ParserHelper.ExtractText(doc, _options.LocationSelector);

                // Parse related entities
                offer.Employer = ParserHelper.CreateEmployer(
                    ParserHelper.ExtractText(doc, _options.EmployerNameSelector)
                );

                offer.WorkMode = ParserHelper.CreateWorkMode(
                    ParserHelper.ExtractText(doc, _options.WorkModeSelector)
                );

                offer.PositionLevel = ParserHelper.CreatePositionLevel(
                    ParserHelper.ExtractText(doc, _options.PositionLevelSelector)
                );

                // Parse technologies
                var technologies = ParserHelper.ExtractNodeList(doc, _options.TechnologiesSelector);
                offer.Technologies = ParserHelper.CreateTechnologies(technologies);

                // Parse contract details
                var contractType = ParserHelper.ExtractText(doc, _options.ContractTypeSelector);

                decimal? minSalary = ParserHelper.TryParseDecimal(
                    ParserHelper.ExtractText(doc, _options.MinSalarySelector)
                );

                decimal? maxSalary = ParserHelper.TryParseDecimal(
                    ParserHelper.ExtractText(doc, _options.MaxSalarySelector)
                );

                var currency = ParserHelper.ExtractText(doc, _options.CurrencySelector);
                var timeUnit = ParserHelper.ExtractText(doc, _options.TimeUnitSelector);

                offer.ContractDetails = ParserHelper.CreateContractDetails(
                    contractType, minSalary, maxSalary, currency, timeUnit
                );

                return offer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing HTML content");
                throw;
            }
        }
    }
}
