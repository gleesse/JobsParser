using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class NavigateCommand(string url, string waitUntil) : Command
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedUrl = ResolveVariables(url, context);
            var waitUntilOption = ParseWaitUntilOption(waitUntil);

            await page.GotoAsync(resolvedUrl, new PageGotoOptions
            {
                WaitUntil = waitUntilOption
            });
        }

        private static WaitUntilState ParseWaitUntilOption(string waitUntil)
        {
            return waitUntil switch
            {
                "load" => WaitUntilState.Load,
                "domcontentloaded" => WaitUntilState.DOMContentLoaded,
                "networkidle" => WaitUntilState.NetworkIdle,
                _ => WaitUntilState.Load
            };
        }
    }
}