using JobParsers.Infrastructure.Queue;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Http;
using JobsParser.Infrastructure.Parsers.Link;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobsParser.LinkParserService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var queueService = host.Services.GetRequiredService<IQueueService>();
            logger.LogInformation("LinkParserApp starting...");

            var linkParserService = host.Services.GetRequiredService<IOfferLinkParser>();
            var uri = "https://it.pracuj.pl/praca/praca%20zdalna;wm,home-office?et=17%2C4&its=backend&itth=39%2C75";

            try
            {
                var offerLinks = await linkParserService.ParseAsync(new Uri(uri), default);

                foreach (var link in offerLinks)
                {
                    await queueService.PublishAsync("offer_details_queue", link);
                    logger.LogInformation($"Published offer link to queue: {link.SourceUrl}");
                }

                logger.LogInformation($"Link parsing completed for URL: {uri}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred during link parsing from URL: {uri}");
            }

            logger.LogInformation("LinkParserApp completed. Press any key to exit.");
            Console.ReadKey();

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((hostingContext, config) =>
               {
                   config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
               })
               .ConfigureServices((hostContext, services) =>
               {
                   // Configure logging
                   services.AddLogging(builder =>
                   {
                       builder.AddConsole();
                       // Add other log providers as needed
                   });
                   services.AddHttpClient("default", (client) =>
                       client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")
                   );
                   services.AddTransient<IHttpClientWrapper, HttpClientWrapper>();
                   services.AddTransient<IOfferLinkParser, PracujLinkParser>();

                   // Configuration of RabbitMQ
                   services.Configure<RabbitSettings>(hostContext.Configuration.GetSection("RabbitSettings"));
                   services.AddSingleton<IQueueService, RabbitMqService>();
               });
    }
}