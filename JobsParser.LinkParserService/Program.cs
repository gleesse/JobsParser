using JobParsers.Infrastructure.Queue;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobsParser.LinkParserService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var queueService = host.Services.GetRequiredService<IQueueService>();
            var parserFactory = host.Services.GetRequiredService<IParserFactory>();
            var websites = host.Services.GetRequiredService<IOptions<List<WebsiteConfiguration>>>().Value;
            var rabbitSettings = host.Services.GetRequiredService<IOptions<RabbitSettings>>().Value;
            logger.LogInformation("LinkParserApp starting...");

            foreach (var website in websites)
            {
                await ProcessWebsite(website, parserFactory, queueService, logger, rabbitSettings);
            }

            logger.LogInformation("LinkParserApp completed. Press any key to exit.");
            Console.ReadKey();

            await host.RunAsync();
        }

        private static async Task ProcessWebsite(WebsiteConfiguration website, IParserFactory factory, IQueueService queueService, ILogger logger, RabbitSettings rabbitSettings)
        {
            try
            {
                var parser = factory.GetLinkParser(website.LinkParserOptions);
                var offerLinks = parser.ParseOfferLinksFromWebsite(website);
                logger.LogInformation($"Parsed {offerLinks.Count()} links for website: {website.SiteUrl}");

                foreach (var link in offerLinks)
                {
                    await queueService.PublishAsync(rabbitSettings.LinksQueue, link);
                    logger.LogInformation($"Published offer link to queue: {link.SourceUrl}");
                }

                logger.LogInformation($"Link parsing completed for URL: {string.Join(';', website.SearchUrls)}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred during link parsing for website: {website.SiteUrl}");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((hostingContext, config) =>
               {
                   config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
               })
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddLogging(builder =>
                   {
                       builder.AddConsole();
                   });
                   services.AddHttpClient("default", (client) =>
                       client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")
                   );

                   services.Configure<List<WebsiteConfiguration>>(hostContext.Configuration.GetSection("Websites"));
                   services.Configure<RabbitSettings>(hostContext.Configuration.GetSection("RabbitSettings"));
                   services.AddSingleton<IQueueService, RabbitMqService>();
                   services.AddSingleton<IParserFactory, DefaultParserFactory>();

                   services.AddSingleton<IPage>(provider =>
                   {
                       var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
                       var browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true })
                                               .GetAwaiter().GetResult();
                       var context = browser.NewContextAsync(new BrowserNewContextOptions
                       {
                           UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36"
                       }).GetAwaiter().GetResult();
                       return context.NewPageAsync().GetAwaiter().GetResult();
                   });
               });
    }
}