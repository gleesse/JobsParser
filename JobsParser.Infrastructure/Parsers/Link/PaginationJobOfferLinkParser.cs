using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;

namespace JobsParser.Infrastructure.Parsers.Link
{
    public class PaginationJobOfferLinkParser : IOfferLinkParser
    {
        private readonly IPage _page;
        private readonly ILogger<PaginationJobOfferLinkParser> _logger;

        public PaginationJobOfferLinkParser(ILogger<PaginationJobOfferLinkParser> logger, IPage page)
        {
            _logger = logger;
            _page = page;
        }

        public IEnumerable<OfferLinkDto> ParseOfferLinksFromWebsite(WebsiteConfiguration website)
        {
            try
            {
                if (string.IsNullOrEmpty(website.LinkParserOptions.NextPageButtonSelector))
                {
                    throw new ArgumentException($"Pagination requires {nameof(LinkParserOptions.NextPageButtonSelector)}.");
                }
                if (string.IsNullOrEmpty(website.LinkParserOptions.ItemSelector))
                {
                    throw new ArgumentException($"Pagination requires {nameof(LinkParserOptions.ItemSelector)}.");
                }

                var targetUrl = !website.SearchUrls.IsNullOrEmpty() ? website.SearchUrls : throw new ArgumentNullException(nameof(website));
                var totalLinks = new List<OfferLinkDto>();

                website.SearchUrls.ToList().ForEach(searchUrl =>
                {
                    var links = ParseLinksFromUrlAsync(searchUrl, website.LinkParserOptions).Result;
                    totalLinks.AddRange(links);
                });

                return totalLinks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during parsing.");
                throw;
            }
        }

        private async Task<IEnumerable<OfferLinkDto>> ParseLinksFromUrlAsync(string searchUrl, LinkParserOptions options)
        {
            _logger.LogInformation("Navigating to URL: {Url}", searchUrl);
            await _page.GotoAsync(searchUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var links = new List<OfferLinkDto>();

            while (true)
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await DeleteDialogElementsAsync();
                await ClickAsync(options.ClickWhenLoadedSelector);

                var extractedLinks = await ExtractLinksFromCurrentPageAsync(options);
                links.AddRange(extractedLinks);
                _logger.LogInformation("Extracted {Count} links. Total links so far: {TotalLinks}", extractedLinks.Count(), links.Count);

                var nextPageElements = await _page.QuerySelectorAllAsync(options.NextPageButtonSelector);
                if (nextPageElements == null || nextPageElements.Count == 0)
                {
                    _logger.LogInformation("No further page found. Stopping pagination.");
                    break;
                }

                await DeleteDialogElementsAsync();
                await ClickAsync(options.NextPageButtonSelector);

                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            return links;
        }

        private async Task<IEnumerable<OfferLinkDto>> ExtractLinksFromCurrentPageAsync(LinkParserOptions options)
        {
            var links = new List<OfferLinkDto>();
            try
            {
                var items = await _page.QuerySelectorAllAsync(options.ItemSelector);
                _logger.LogInformation("Found {ItemCount} items on page.", items.Count);

                foreach (var item in items)
                {
                    string href = await item.GetAttributeAsync("href");
                    if (!string.IsNullOrWhiteSpace(href) &&
                        Uri.TryCreate(new Uri(_page.Url), href, out var absoluteUri))
                    {
                        var link = new OfferLinkDto()
                        {
                            SourceUrl = absoluteUri
                        };
                        links.Add(link);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting links from current page.");
            }
            return links;
        }

        private async Task<int> ClickAsync(string selector)
        {
            try
            {
                _logger.LogInformation($"Find and click elements for selector: {selector}.");
                var elements = (await _page.QuerySelectorAllAsync(selector)).ToList();
                if (elements != null && elements.Count > 0)
                {
                    _logger.LogInformation($"{elements.Count} elements found and to be clicked");
                    await DeleteDialogElementsAsync();

                    var clickTasks = elements.Select(async element =>
                    {
                        await element.ClickAsync();
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    });

                    await Task.WhenAll(clickTasks);
                }
                _logger.LogInformation($"Element not found.");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clicking the element.");
                throw ex;
            }
        }

        private async Task DeleteDialogElementsAsync()
        {
            string[] popupSelectors =
            [
                @"[role=""dialog""]",
                "#popupContainer"
            ];

            int totalRemoved = 0;

            foreach (var selector in popupSelectors)
            {
                try
                {
                    var removeScript = $@"
                    () => {{
                        const elements = document.querySelectorAll('{selector}');
                        elements.forEach(el => el.remove());
                        return elements.length;
                    }}";

                    int removedCount = await _page.EvaluateAsync<int>(removeScript);

                    totalRemoved += removedCount;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error removing elements with selector: {selector}");
                }
            }

            if (totalRemoved > 0)
            {
                _logger.LogInformation($"Removed {totalRemoved} popup elements");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }
    }
}
