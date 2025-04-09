using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Database;
using JobsParser.Infrastructure.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobsParser.LinkParserService
{
    public class LinkParserBackgroundService(
        ILogger<LinkParserBackgroundService> logger,
        IOptions<List<WebsiteConfiguration>> websiteConfigurations,
        IParserFactory parserFactory,
        IQueueService queueService,
        AppDbContext dbContext,
        IOptions<RabbitSettings> rabbitSettings,
        IOptions<LinkParserServiceSettings> serviceSettings) : BackgroundService
    {
        private readonly ILogger<LinkParserBackgroundService> _logger = logger;
        private readonly List<WebsiteConfiguration> _websiteConfigurations = websiteConfigurations != default ? websiteConfigurations.Value : throw new ArgumentNullException(nameof(websiteConfigurations));
        private readonly IParserFactory _parserFactory = parserFactory;
        private readonly IQueueService _queueService = queueService;
        private readonly AppDbContext _dbContext = dbContext;
        private readonly RabbitSettings _rabbitSettings = rabbitSettings != default ? rabbitSettings.Value : throw new ArgumentNullException(nameof(rabbitSettings));
        private readonly LinkParserServiceSettings _serviceSettings = serviceSettings != default ? serviceSettings.Value : throw new ArgumentNullException(nameof(serviceSettings));
        private TimeSpan ExecutionInterval => TimeSpan.FromHours(_serviceSettings.ExecutionIntervalHours);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LinkParser Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Link parsing job started at: {time}", DateTimeOffset.Now);

                try
                {
                    await ProcessWebsitesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing the link parsing job");
                }

                _logger.LogInformation("Link parsing job completed. Next run at: {time}", DateTimeOffset.Now.Add(ExecutionInterval));
                await Task.Delay(ExecutionInterval, stoppingToken);
            }
        }

        private async Task ProcessWebsitesAsync(CancellationToken cancellationToken)
        {
            foreach (var website in _websiteConfigurations)
            {
                await ProcessWebsiteAsync(website, cancellationToken);
            }
        }

        private async Task ProcessWebsiteAsync(WebsiteConfiguration website, CancellationToken cancellationToken)
        {
            try
            {
                var parser = _parserFactory.GetLinkParser(website.LinkParserOptions);
                var offerLinks = parser.ParseOfferLinksFromWebsite(website);
                _logger.LogInformation($"Parsed {offerLinks.Count()} links for website: {website.SiteUrl}");

                if(website.LinkParserOptions.RemoveQuery)
                {
                    offerLinks.ToList().ForEach(offerLink => offerLink.SourceUrl = new Uri(offerLink.SourceUrl.GetLeftPart(UriPartial.Path)));
                }

                int newLinks = 0;
                int existingLinks = 0;

                foreach (var link in offerLinks)
                {
                    // Check if offer already exists in the database
                    if (await _dbContext.OfferExistsAsync(link.SourceUrl.ToString()))
                    {
                        existingLinks++;
                        _logger.LogInformation($"Offer already exists in database: {link.SourceUrl}");
                        continue;
                    }

                    // Only add new offers to the queue
                    await _queueService.PublishAsync(_rabbitSettings.LinksQueue, link);
                    _logger.LogInformation($"Published new offer link to queue: {link.SourceUrl}");
                    newLinks++;
                }

                _logger.LogInformation($"Link parsing completed for {website.SiteUrl}. New links: {newLinks}, Existing links: {existingLinks}");
            }
            catch (HttpTooManyRequestsException ex)
            {
                _logger.LogError($"Received HTTP 429 Too Many Requests from: {ex.TargetUrl}");
                await HandleTooManyRequestsErrorAsync(ex, _serviceSettings, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred during link parsing for website: {website.SiteUrl}");
            }
        }

        private static async Task HandleTooManyRequestsErrorAsync(HttpTooManyRequestsException tooManyRequestsEx, LinkParserServiceSettings serviceSettings, ILogger logger)
        {
            if (tooManyRequestsEx.Delay != null)
            {
                var retryAfterMinutes = tooManyRequestsEx.Delay.Value.TotalMinutes;
                logger.LogInformation($"Retrying after {retryAfterMinutes} minutes.");
                await Task.Delay(tooManyRequestsEx.Delay.Value);
            }
            else
            {
                int currentAttempt = 1;

                while (currentAttempt <= serviceSettings.MaxRetryAttempts)
                {
                    int delayMinutes = serviceSettings.InitialRetryDelayHours * 60 * (int)Math.Pow(2, currentAttempt - 1);
                    logger.LogInformation($"Retry-After header not found. Using exponential backoff delay of {delayMinutes} minutes. Attempt {currentAttempt} of {serviceSettings.MaxRetryAttempts}");
                    await Task.Delay(delayMinutes * 60000); // Convert to milliseconds
                    currentAttempt++;
                }
            }
        }
    }
}
