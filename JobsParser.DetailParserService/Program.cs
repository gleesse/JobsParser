using JobParsers.Infrastructure.Queue;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Database;
using JobsParser.Infrastructure.Factories;
using JobsParser.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobsParser.DetailParserService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("DetailParserApp starting...");

            var queueService = host.Services.GetRequiredService<IQueueService>();
            var parserFactory = host.Services.GetRequiredService<IParserFactory>();
            var dbContext = host.Services.GetRequiredService<AppDbContext>();
            var websites = host.Services.GetRequiredService<IOptions<List<WebsiteConfiguration>>>().Value;
            var rabbitSettings = host.Services.GetRequiredService<IOptions<RabbitSettings>>().Value;

            await queueService.ConsumeAsync<OfferLinkDto>(rabbitSettings.LinksQueue, async (offerLink) =>
            {
                logger.LogInformation($"Received offer link: {offerLink.SourceUrl}");
                var detailParserOptions = websites.FirstOrDefault(website => website.SiteUrl == offerLink.SourceUrl.Host)?.DetailParserOptions;
                if (detailParserOptions is null) throw new ArgumentException($"Detail parser is not configured for such website: {offerLink.SourceUrl.Host}");

                var offerExistsInDatabase = await dbContext.OfferExistsAsync(offerLink.SourceUrl.ToString());

                if (!offerExistsInDatabase)
                {
                    try
                    {
                        logger.LogInformation($"Offer link not found in a database. Starting parsing: {offerLink.SourceUrl}");
                        var parser = parserFactory.GetDetailParser(detailParserOptions);
                        var offer = await parser.ParseAsync(offerLink.SourceUrl);
                        await dbContext.SaveOfferAsync(offer);
                        logger.LogInformation($"Successfully parsed and saved offer from: {offerLink.SourceUrl}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error parsing or saving offer from: {offerLink.SourceUrl}");
                        // Future improvement: Add to a dead-letter queue for retry
                    }
                }
                else
                {
                    logger.LogInformation($"Offer already exists in database: {offerLink.SourceUrl}");
                }

                logger.LogInformation($"Processed offer link: {offerLink.SourceUrl}");
            });

            logger.LogInformation("DetailParserApp is running. Press any key to exit.");
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

                    // Database Configuration
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlServer(hostContext.Configuration.GetSection("DatabaseSettings")["ConnectionString"]);
                    });

                    // Add HttpClientFactory
                    services.AddHttpClient("default", (client) =>
                       client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")
                    );

                    // Add Dependencies
                    services.AddTransient<IHttpClientWrapper, HttpClientWrapper>();
                    services.Configure<List<WebsiteConfiguration>>(hostContext.Configuration.GetSection("Websites"));

                    // Configuration of RabbitMQ
                    services.Configure<RabbitSettings>(hostContext.Configuration.GetSection("RabbitSettings"));
                    services.AddSingleton<IQueueService, RabbitMqService>();
                    services.AddSingleton<IParserFactory, DefaultParserFactory>();
                });
    }
}