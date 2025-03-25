using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public class HtmlDetailParser : IOfferDetailParser
    {
        public Task<OfferDto> ParseAsync(Uri offerUrl, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }


        private readonly HttpClient _httpClient;

        public HtmlDetailParser()
        {
            _httpClient = new HttpClient();
        }

        public HtmlDetailParser(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        //public async Task<OfferDto> ParseOfferAsync(string url, DetailParserOptions options)
        //{
        //    try
        //    {
        //        var html = await _httpClient.GetStringAsync(url);
        //        return ParseOffer(html, url, options);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error fetching or parsing URL: {url}. Error: {ex.Message}");
        //        throw;
        //    }
        //}

        //public OfferDto ParseOffer(string html, string sourceUrl, DetailParserOptions options)
        //{
        //    var doc = new HtmlDocument();
        //    doc.LoadHtml(html);
        //
        //    var offer = new OfferDto
        //    {
        //        OfferUrl = sourceUrl
        //    };
        //
        //    // Parse single value fields
        //    offer.Title = ExtractText(doc, options.JobTitleSelector);
        //    offer.ApplicationUrl = ExtractAttribute(doc, options.ApplicationUrlSelector, "href");
        //    offer.PositionLevels = ExtractText(doc, options.PositionLevelSelector);
        //    offer.WorkModes = ExtractText(doc, options.WorkModeSelector);
        //    offer.Location = ExtractText(doc, options.PlaceSelector);
        //    offer.AboutUs = ExtractText(doc, options.AboutProjectSelector);
        //    offer.Responsibilities = ExtractText(doc, options.ResponsibilitiesSelector);
        //    offer.Requirements = ExtractText(doc, options.RequirementsSelector);
        //
        //    // Parse dates
        //    if (!string.IsNullOrEmpty(options.DateCreatedSelector))
        //    {
        //        var dateText = ExtractText(doc, options.DateCreatedSelector);
        //        if (DateTime.TryParse(dateText, out DateTime dateCreated) ||
        //            DateTime.TryParseExact(dateText, options.DateFormat, null, System.Globalization.DateTimeStyles.None, out dateCreated))
        //        {
        //            offer.DateCreated = dateCreated;
        //        }
        //    }
        //
        //    if (!string.IsNullOrEmpty(options.ExpirationDateSelector))
        //    {
        //        var expirationText = ExtractText(doc, options.ExpirationDateSelector);
        //        if (DateTime.TryParse(expirationText, out DateTime expirationDate) ||
        //            DateTime.TryParseExact(expirationText, options.DateFormat, null, System.Globalization.DateTimeStyles.None, out expirationDate))
        //        {
        //            offer.ExpirationDate = expirationDate;
        //        }
        //    }
        //
        //    // Parse lists
        //    if (!string.IsNullOrEmpty(options.TechnologiesSelector))
        //    {
        //        var techList = doc.DocumentNode.SelectSingleNode(options.TechnologiesSelector);
        //        if (techList != null)
        //        {
        //            if (!string.IsNullOrEmpty(options.TechnologyItemSelector))
        //            {
        //                var techItems = techList.SelectNodes(options.TechnologyItemSelector);
        //                if (techItems != null)
        //                {
        //                    foreach (var item in techItems)
        //                    {
        //                        offer.Technologies.Add(item.InnerText.Trim());
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                offer.Technologies.Add(techList.InnerText.Trim());
        //            }
        //        }
        //    }
        //
        //    if (!string.IsNullOrEmpty(options.BenefitsSelector))
        //    {
        //        var benefitsList = doc.DocumentNode.SelectSingleNode(options.BenefitsSelector);
        //        if (benefitsList != null)
        //        {
        //            if (!string.IsNullOrEmpty(options.BenefitItemSelector))
        //            {
        //                var benefitItems = benefitsList.SelectNodes(options.BenefitItemSelector);
        //                if (benefitItems != null)
        //                {
        //                    foreach (var item in benefitItems)
        //                    {
        //                        offer.Benefits.Add(item.InnerText.Trim());
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                offer.Benefits.Add(benefitsList.InnerText.Trim());
        //            }
        //        }
        //    }
        //
        //    return offer;
        //}

        private string ExtractText(HtmlDocument doc, string selector)
        {
            if (string.IsNullOrEmpty(selector))
                return null;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.InnerText.Trim();
        }

        private string ExtractAttribute(HtmlDocument doc, string selector, string attributeName)
        {
            if (string.IsNullOrEmpty(selector) || string.IsNullOrEmpty(attributeName))
                return null;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.GetAttributeValue(attributeName, null);
        }
    }
}
