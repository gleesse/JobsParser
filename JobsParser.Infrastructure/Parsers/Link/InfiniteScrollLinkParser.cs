using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace JobsParser.Infrastructure.Parsers.Link
{
    public class InfiniteScrollJobOfferLinkParser : IOfferLinkParser
    {
        private readonly IPage _page;
        private readonly ILogger<InfiniteScrollJobOfferLinkParser> _logger;

        public InfiniteScrollJobOfferLinkParser(ILogger<InfiniteScrollJobOfferLinkParser> logger)
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
                if (string.IsNullOrEmpty(website.LinkParserOptions.ItemSelector))
                {
                    throw new ArgumentException($"Infinite scroll requires {nameof(LinkParserOptions.ItemSelector)}.");
                }

                var targetUrls = website.SearchUrls ?? throw new ArgumentNullException(nameof(website.SearchUrls));
                var totalLinks = new List<OfferLinkDto>();

                foreach (var searchUrl in targetUrls)
                {
                    var links = ParseLinksFromUrlAsync(searchUrl, website.LinkParserOptions).Result;
                    totalLinks.AddRange(links);
                }

                return totalLinks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during parsing with infinite scroll.");
                throw;
            }
        }

        private async Task<IEnumerable<OfferLinkDto>> ParseLinksFromUrlAsync(string searchUrl, LinkParserOptions options)
        {
            _logger.LogInformation("Navigating to URL: {Url}", searchUrl);
            await _page.GotoAsync(searchUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var links = new HashSet<OfferLinkDto>(new UriComparer());
            int previousLinkCount = 0;
            int sameCountIterations = 0;
            const int maxAttempts = 10; // Maximum attempts if no new links are found
            const int scrollAttempts = 5; // Number of scroll attempts before checking for new links

            // Dismiss any initial dialogs or popups
            await DeleteDialogElementsAsync();

            // Click on any required elements when the page loads
            if (!string.IsNullOrEmpty(options.ClickWhenLoadedSelector))
            {
                await ClickAsync(options.ClickWhenLoadedSelector);
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            while (sameCountIterations < maxAttempts)
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Perform multiple scrolls before extracting links
                for (int i = 0; i < scrollAttempts; i++)
                {
                    await ScrollPageAsync();
                    await Task.Delay(1000); // Wait for content to load
                }

                // Extract links after scrolling
                var extractedLinks = await ExtractLinksFromCurrentPageAsync(options);
                foreach (var link in extractedLinks)
                {
                    links.Add(link);
                }

                _logger.LogInformation("Extracted {Count} links. Total unique links so far: {TotalLinks}",
                    extractedLinks.Count(), links.Count);

                // Check if we've found any new links
                if (links.Count == previousLinkCount)
                {
                    sameCountIterations++;
                    _logger.LogInformation("No new links found. Attempt {Attempt} of {MaxAttempts}",
                        sameCountIterations, maxAttempts);
                }
                else
                {
                    sameCountIterations = 0;
                    previousLinkCount = links.Count;
                }

                // Dismiss any dialogs that may have appeared
                await DeleteDialogElementsAsync();
            }

            _logger.LogInformation("Finished infinite scrolling on {Url}, found {Count} unique links",
                searchUrl, links.Count);
            return links;
        }

        private async Task ScrollPageAsync()
        {
            try
            {
                // Scroll to the bottom of the page
                await _page.EvaluateAsync(@"
                    () => {
                        window.scrollBy(0, window.innerHeight);
                        return document.body.scrollHeight;
                    }
                ");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scrolling page");
            }
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
                var elements = await _page.QuerySelectorAllAsync(selector);
                if (elements != null && elements.Count > 0)
                {
                    _logger.LogInformation($"{elements.Count} elements found and to be clicked");
                    await elements[0].ClickAsync();
                    await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
                "#popupContainer",
                ".cookie-popup",
                ".modal",
                ".consent-popup"
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

    // Helper class to compare URIs for the HashSet to avoid duplicate links
    internal class UriComparer : IEqualityComparer<OfferLinkDto>
    {
        public bool Equals(OfferLinkDto x, OfferLinkDto y)
        {
            if (x == null || y == null)
                return false;

            return x.SourceUrl.ToString().Equals(y.SourceUrl.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(OfferLinkDto obj)
        {
            if (obj == null || obj.SourceUrl == null)
                return 0;

            return obj.SourceUrl.ToString().ToLowerInvariant().GetHashCode();
        }
    }
}
