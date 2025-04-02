using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ElementExistsCommand(string selector, int? customTimeout = null) : Command
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedSelector = ResolveVariables(selector, context);
            var options = CreateClickOptions(customTimeout);

            try
            {
                var element = await page.WaitForSelectorAsync(resolvedSelector, options);
                context.SetVariable("ConditionResult", element != null);
            }
            catch (TimeoutException)
            {
                context.SetVariable("ConditionResult", false);
            }
        }

        private static PageWaitForSelectorOptions CreateClickOptions(int? timeout)
        {
            var clickOptions = new PageWaitForSelectorOptions();
            clickOptions.State = WaitForSelectorState.Attached;

            if (timeout.HasValue)
            {
                clickOptions.Timeout = timeout.Value;
            }

            return clickOptions;
        }
    }
}