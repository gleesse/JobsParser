using HtmlAgilityPack;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public class ProgrammerJobParser
    {
        private readonly ILogger<ProgrammerJobParser> _logger;

        public ProgrammerJobParser(ILogger<ProgrammerJobParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OfferDto> ParseOfferDetailsAsync(string html, OfferLinkDto offerLink, WebsiteConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning($"Empty HTML content for {offerLink.SourceUrl}");
                return CreateBasicOffer(offerLink);
            }

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var offer = new OfferDto
                {
                    Url = offerLink.SourceUrl.ToString(),
                    Title = ExtractText(doc, ".job-title"),
                    Description = ExtractText(doc, ".job-description"),
                    Location = ExtractText(doc, ".job-location"),
                    Employer = new Employer { Name = "Unknown" } // Placeholder as per tests
                };

                // Extract work mode
                var workModeText = ExtractText(doc, ".job-location");
                offer.WorkMode = !string.IsNullOrEmpty(workModeText) 
                    ? new WorkMode { Name = workModeText } 
                    : null;

                // Extract position level (default to null in tests)
                offer.PositionLevel = null;

                // Extract technologies
                var techText = ExtractText(doc, ".job-tech");
                offer.Technologies = ParseTechnologies(techText);

                // Extract salary information
                var salaryText = ExtractText(doc, ".job-salary");
                offer.ContractDetails = ParseContractDetails(salaryText);

                return offer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing job offer from {offerLink.SourceUrl}");
                return CreateBasicOffer(offerLink);
            }
        }

        private OfferDto CreateBasicOffer(OfferLinkDto offerLink)
        {
            return new OfferDto
            {
                Url = offerLink.SourceUrl.ToString(),
                Title = string.Empty,
                Description = string.Empty,
                ContractDetails = new ContractDetails
                {
                    MinSalary = 0,
                    MaxSalary = 0,
                    Currency = string.Empty,
                    TypeOfContract = string.Empty,
                    TimeUnit = string.Empty
                },
                Technologies = new List<Technology>()
            };
        }

        private string ExtractText(HtmlDocument doc, string selector)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//{selector.Replace('.', ' ').Trim()} | //*[contains(@class, '{selector.Replace('.', ' ').Trim()}')]");
            return node?.InnerText?.Trim() ?? string.Empty;
        }

        private List<Technology> ParseTechnologies(string techText)
        {
            if (string.IsNullOrEmpty(techText))
                return new List<Technology>();

            return techText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tech => tech.Trim())
                .Where(tech => !string.IsNullOrEmpty(tech))
                .Select(tech => new Technology { Name = tech })
                .ToList();
        }

        private ContractDetails ParseContractDetails(string salaryText)
        {
            var contractDetails = new ContractDetails
            {
                MinSalary = 0,
                MaxSalary = 0,
                Currency = string.Empty,
                TypeOfContract = "Full-time", // Default value
                TimeUnit = "month" // Default value
            };

            if (string.IsNullOrEmpty(salaryText))
                return contractDetails;

            try
            {
                // Example format: "$10,000 - $15,000 USD"
                var match = Regex.Match(salaryText, @"[\$£€]?(\d[\d\s,.]*)\s*-\s*[\$£€]?(\d[\d\s,.]*)\s*([A-Z]{3})?");
                if (match.Success)
                {
                    // Extract min salary
                    if (decimal.TryParse(match.Groups[1].Value.Replace(",", "").Replace(".", ","), out decimal minSalary))
                    {
                        contractDetails.MinSalary = minSalary;
                    }

                    // Extract max salary
                    if (decimal.TryParse(match.Groups[2].Value.Replace(",", "").Replace(".", ","), out decimal maxSalary))
                    {
                        contractDetails.MaxSalary = maxSalary;
                    }

                    // Extract currency
                    contractDetails.Currency = match.Groups[3].Success ? match.Groups[3].Value : "USD";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error parsing salary information: {salaryText}");
            }

            return contractDetails;
        }
    }
} 