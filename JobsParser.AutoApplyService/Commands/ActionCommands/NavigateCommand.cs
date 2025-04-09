using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class NavigateCommand : Command
    {
        private readonly string _url;
        private readonly string _waitUntil;

        public NavigateCommand(string url, string waitUntil, ILogger<NavigateCommand> logger) : base(logger)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _waitUntil = waitUntil ?? "load";
        }

        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedUrl = ResolveVariables(_url, context);
            _logger.LogInformation("Navigating to URL: {Url} with waitUntil: {WaitUntil}", resolvedUrl, _waitUntil);

            var waitUntilOption = ParseWaitUntilOption(_waitUntil);

            await page.GotoAsync(resolvedUrl, new PageGotoOptions
            {
                WaitUntil = waitUntilOption
            });

            _logger.LogInformation("Successfully navigated to URL: {Url}", resolvedUrl);
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