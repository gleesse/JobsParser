using JobParsers.Infrastructure.Queue;
using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Database;
using JobsParser.Infrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace JobsParser.LinkParserService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
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
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                });
                services.AddHttpClient("default", (client) =>
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")
                );

                services.Configure<List<WebsiteConfiguration>>(hostContext.Configuration.GetSection("Websites"));
                services.Configure<RabbitSettings>(hostContext.Configuration.GetSection("RabbitSettings"));
                services.Configure<LinkParserServiceSettings>(hostContext.Configuration.GetSection("LinkParserServiceSettings"));
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

                // Add DbContext
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(hostContext.Configuration.GetSection("DatabaseSettings")["ConnectionString"]));

                services.AddDbContextFactory<AppDbContext>(options =>
                    options.UseSqlServer(hostContext.Configuration.GetSection("DatabaseSettings")["ConnectionString"]));

                // Add the background service
                services.AddHostedService<LinkParserBackgroundService>();
            });
    }
}