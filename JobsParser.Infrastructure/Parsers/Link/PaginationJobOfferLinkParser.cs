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

        public PaginationJobOfferLinkParser(ILogger<PaginationJobOfferLinkParser> logger)
        {
            _logger = logger;

            var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
            var browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false }).GetAwaiter().GetResult();
            var context = browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36"
            }).Result;

            _page = browser.NewPageAsync().GetAwaiter().GetResult();
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
                bool clickResult = await ClickAsync(options.ClickWhenLoadedSelector);
                if (!clickResult)
                {
                    _logger.LogWarning("Failed to click loaded selector. Breaking loop.");
                    break;
                }

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
                bool nextPageClickResult = await ClickAsync(options.NextPageButtonSelector);
                if (!nextPageClickResult)
                {
                    _logger.LogWarning("Failed to click next page. Breaking loop.");
                    break;
                }

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

        private async Task<bool> ClickAsync(string selector)
        {
            try
            {
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
                    return true;
                }
                _logger.LogInformation("Element not found or not visible.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clicking element for selector: {selector}");
                return false;
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
