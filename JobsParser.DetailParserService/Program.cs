using JobParsers.Infrastructure.Queue;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Database;
using JobsParser.Infrastructure.Http;
using JobsParser.Infrastructure.Parsers.Detail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var offerRepository = host.Services.GetRequiredService<IOfferRepository>();
            var detailParserService = host.Services.GetRequiredService<IOfferDetailParser>();

            await queueService.ConsumeAsync<OfferLinkDto>("offer_details_queue", async (offerLink) =>
            {
                logger.LogInformation($"Received offer link: {offerLink.SourceUrl}");
                var offer = await detailParserService.ParseAsync(offerLink.SourceUrl, default);
                await offerRepository.SaveOfferAsync(offer, default);
                logger.LogInformation($"Processed offer link: {offerLink.SourceUrl}");
            }, default);

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
                    services.AddTransient<IOfferDetailParser, PracujDetailParser>();
                    services.AddTransient<IOfferRepository, OfferRepository>();

                    // Configuration of RabbitMQ
                    services.Configure<RabbitSettings>(hostContext.Configuration.GetSection("RabbitSettings"));
                    services.AddSingleton<IQueueService, RabbitMqService>();
                });
    }
}
