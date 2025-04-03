using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ElementExistsCommand(string selector, int? customTimeout = null) : Command
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedSelector = ResolveVariables(selector, context);

            var seconds = customTimeout ?? 0;
            await page.WaitForTimeoutAsync(seconds);

            var count = await page.Locator(resolvedSelector).CountAsync();
            context.SetVariable("ConditionResult", count > 0);
        }
    }
}