using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ClickCommand(
        string selector,
        ILogger<ClickCommand> logger,
        int? waitForTimeoutSeconds = null,
        int? customTimeout = null,
        bool waitForNetworkIdle = false,
        string? waitForSelector = null) : Command(logger)
    {
        private readonly string _selector = selector ?? throw new ArgumentNullException(nameof(selector));

        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedSelector = ResolveVariables(_selector, context);
            _logger.LogInformation("Clicking element with selector: {Selector}", resolvedSelector);

            var clickOptions = CreateClickOptions(customTimeout);

            if (waitForTimeoutSeconds.HasValue)
            {
                _logger.LogDebug("Will wait for {TimeToWait}s after click", waitForTimeoutSeconds.Value);
                await page.ClickAsync(resolvedSelector, clickOptions);
                var ms = waitForTimeoutSeconds.Value * 1000;
                await page.WaitForTimeoutAsync(ms);
            }
            else if (waitForSelector != null)
            {
                string? resolvedWaitForSelector = waitForSelector != null ? ResolveVariables(waitForSelector, context) : null;
                if (string.IsNullOrEmpty(resolvedWaitForSelector)) return;

                _logger.LogDebug("Will wait for selector after click: {WaitForSelector}", resolvedWaitForSelector);
                await Task.WhenAll(
                    page.ClickAsync(resolvedSelector, clickOptions),
                    page.WaitForSelectorAsync(resolvedWaitForSelector)
                );
            }
            else if (waitForNetworkIdle)
            {
                _logger.LogDebug("Will wait for network idle after click");
                await Task.WhenAll(
                    page.ClickAsync(resolvedSelector, clickOptions),
                    page.WaitForLoadStateAsync(LoadState.NetworkIdle)
                );
            }
            else
            {
                await page.ClickAsync(resolvedSelector, clickOptions);
            }

            _logger.LogDebug("Click operation completed on selector: {Selector}", resolvedSelector);
        }

        private static PageClickOptions CreateClickOptions(int? timeout)
        {
            var clickOptions = new PageClickOptions();
            if (timeout.HasValue)
            {
                clickOptions.Timeout = timeout.Value;
            }

            return clickOptions;
        }
    }
}