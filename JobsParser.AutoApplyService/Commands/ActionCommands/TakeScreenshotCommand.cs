using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class TakeScreenshotCommand(string path, ILogger<TakeScreenshotCommand> logger) : Command(logger)
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            try
            {
                var resolvedPath = ResolveVariables(path, context);
                await page.ScreenshotAsync(new PageScreenshotOptions 
                { 
                    Path = $"{resolvedPath}/{Guid.NewGuid()}.png",
                    FullPage = true
                });
                logger.LogInformation($"Screenshot taken and saved to {resolvedPath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to take screenshot");
                throw;
            }
        }
    }
}
