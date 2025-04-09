using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ElementExistsCommand(string selector, int? customTimeout, ILogger<ElementExistsCommand> logger) : Command(logger)
    {
        private readonly string _selector = selector ?? throw new ArgumentNullException(nameof(selector));

        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedSelector = ResolveVariables(_selector, context);
            _logger.LogInformation("Checking if element exists with selector: {Selector}", resolvedSelector);

            if (customTimeout.HasValue)
            {
                _logger.LogDebug("Waiting for {Timeout}ms before checking element existence", customTimeout.Value);
                await page.WaitForTimeoutAsync(customTimeout.Value);
            }

            var count = await page.Locator(resolvedSelector).CountAsync();
            var exists = count > 0;

            _logger.LogInformation("Element with selector {Selector} exists: {Exists}", resolvedSelector, exists);
            context.SetVariable("ConditionResult", exists);
        }
    }
}