using JobParsers.Infrastructure.Queue;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Database;
using JobsParser.Infrastructure.Factories;
using JobsParser.Infrastructure.Http;
using JobsParser.Infrastructure.Parsers.Detail;
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
            var offerRepository = host.Services.GetRequiredService<IOfferRepository>();
            var websites = host.Services.GetRequiredService<IOptions<List<WebsiteConfiguration>>>().Value;

            await queueService.ConsumeAsync<OfferLinkDto>("offer_details_queue", async (offerLink) =>
            {
                logger.LogInformation($"Received offer link: {offerLink.SourceUrl}");
                var detailParserOptions = websites.FirstOrDefault(website => website.SiteUrl == offerLink.SourceUrl.Host)?.DetailParserOptions;
                if (detailParserOptions is null) throw new ArgumentException($"Detail parser is not configured for such website: {offerLink.SourceUrl.Host}");

                var offerExistsInDatabae = await offerRepository.OfferExistsAsync(offerLink.SourceUrl.ToString());

                if (!offerExistsInDatabae)
                {
                    logger.LogInformation($"Offer link not found in a database. Starting parsing: {offerLink.SourceUrl}");
                    var parser = parserFactory.GetDetailParser(detailParserOptions);
                    //TODO парсит хорошо и записывает тоже, но так как каждый раз создается новый емплоер, воркмод, технологии и тд
                    //создаются дубликаты. Надо сделать чтобы он по названию искал в базе и если нет то добавлял
                    var offer = await parser.ParseAsync(offerLink.SourceUrl, default);
                    await offerRepository.SaveOfferAsync(offer, default);
                }
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
                    services.Configure<List<WebsiteConfiguration>>(hostContext.Configuration.GetSection("Websites"));

                    // Configuration of RabbitMQ
                    services.Configure<RabbitSettings>(hostContext.Configuration.GetSection("RabbitSettings"));
                    services.AddSingleton<IQueueService, RabbitMqService>();
                    services.AddSingleton<IParserFactory, DefaultParserFactory>();
                });
    }
}