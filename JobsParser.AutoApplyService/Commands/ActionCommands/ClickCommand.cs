using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ClickCommand(string selector, int? customTimeout = null, bool waitForNavigation = false) : Command
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedSelector = ResolveVariables(selector, context);
            var clickOptions = CreateClickOptions(customTimeout);

            if (waitForNavigation)
            {
                await Task.WhenAll(
                    page.ClickAsync(resolvedSelector, clickOptions),
                    page.WaitForLoadStateAsync(LoadState.NetworkIdle)
                );
            }
            else
            {
                await page.ClickAsync(resolvedSelector, clickOptions);
            }
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