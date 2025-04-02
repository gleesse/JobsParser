using JobsParser.AutoApplyService.Commands;
using JobsParser.AutoApplyService.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.DSL
{
    public class WorkflowExecutor(ILogger<WorkflowExecutor> logger, IOptions<PlaywrightOptions> options)
    {
        private readonly PlaywrightOptions _options = options.Value;

        public async Task<CommandContext> ExecuteWorkflowAsync(Command workflow, CommandContext? initialContext = null, bool closeBrowserWhenFinished = true)
        {
            logger.LogInformation("Starting workflow execution");

            var context = initialContext ?? new CommandContext();

            using var playwright = await Playwright.CreateAsync();
            var browserOptions = new BrowserTypeLaunchOptions();
            if (_options.Headless)
            {
                browserOptions.Headless = _options.Headless;
            }
            if (!string.IsNullOrEmpty(_options.BrowserChannel))
            {
                browserOptions.Channel = _options.BrowserChannel;
            }
            await using var browser = await playwright.Chromium.LaunchAsync();

            try
            {
                var contextOptions = new BrowserNewContextOptions();

                if (_options.UseSavedCookies)
                {
                    string domain = GetDomainFromContext(context);
                    string cookiePath = GetCookiePath(domain);
                    contextOptions.StorageStatePath = cookiePath;
                }

                if (!string.IsNullOrEmpty(_options.UserAgent))
                {
                    contextOptions.UserAgent = _options.UserAgent;
                    logger.LogInformation($"Using custom user agent: {_options.UserAgent}");
                }

                await using var browserContext = await browser.NewContextAsync(contextOptions);
                var page = await browserContext.NewPageAsync();

                await workflow.ExecuteAsync(page, context);

                logger.LogInformation("Workflow completed successfully");

                return context;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing workflow");
                throw;
            }
            finally
            {
                if (closeBrowserWhenFinished)
                    await browser.CloseAsync();
            }
        }

        private string GetDomainFromContext(CommandContext context)
        {
            if (context.TryGetVariable("JobUrl", out string? jobUrl) && !string.IsNullOrEmpty(jobUrl))
            {
                try
                {
                    var uri = new Uri(jobUrl);
                    return uri.Host;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Failed to extract domain from URL: {jobUrl}");
                }
            }

            return "default";
        }

        private string GetCookiePath(string domain)
        {
            return Path.Combine(
                _options.CookiesDirectory ?? "Cookies",
                $"{domain}.json"
            );
        }
    }
}