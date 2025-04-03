using JobsParser.AutoApplyService.Commands;
using JobsParser.AutoApplyService.DSL;
using JobsParser.AutoApplyService.Models;
using JobsParser.AutoApplyService.Repositories;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobsParser.AutoApplyService
{
    public class AutoApplyBackgroundService : BackgroundService
    {
        private readonly ILogger<AutoApplyBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AutoApplyServiceOptions _options;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly WorkflowExecutor _workflowExecutor;

        public AutoApplyBackgroundService(
            ILogger<AutoApplyBackgroundService> logger,
            IServiceProvider serviceProvider,
            IOptions<AutoApplyServiceOptions> options,
            IWorkflowRepository workflowRepository,
            WorkflowExecutor workflowExecutor)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _workflowRepository = workflowRepository;
            _workflowExecutor = workflowExecutor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Apply Service starting");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessPendingApplicationsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing pending applications");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, no action needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Auto Apply Service");
            }

            _logger.LogInformation("Auto Apply Service stopping");
        }

        private async Task ProcessPendingApplicationsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Processing pending applications");

            // Get job links that haven't been applied to yet
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var pendingLinks = await dbContext.Offers
                //.Where(link => !link.IsApplied && link.IsProcessed) todo
                //.OrderBy(link => link.CreatedAt)
                .Take(_options.MaxConcurrentInstances)
                .ToListAsync(stoppingToken);

            if (!pendingLinks.Any())
            {
                _logger.LogInformation("No pending applications found");
                return;
            }

            _logger.LogInformation("Found {Count} pending applications", pendingLinks.Count);

            // Process each link in parallel, limited by MaxConcurrentInstances
            var tasks = pendingLinks.Select(link => ProcessJobOfferAsync(link, stoppingToken));
            await Task.WhenAll(tasks);
        }

        private async Task ProcessJobOfferAsync(OfferDto jobOffer, CancellationToken stoppingToken)
        {
            ArgumentNullException.ThrowIfNull(jobOffer, nameof(jobOffer));

            _logger.LogInformation("Processing job link: {Url}", jobOffer.Url);

            try
            {
                var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

                string workflowName = DetermineWorkflowName(jobOffer.Url);
                var workflow = await _workflowRepository.GetWorkflowAsync(workflowName);

                // Create initial context with job data
                var context = new CommandContext();
                context.SetVariable("JobUrl", jobOffer.Url);
                context.SetVariable("JobTitle", jobOffer.Title);
                context.SetVariable("JobId", jobOffer.Id.ToString());
                context.SetVariable("CompanyName", jobOffer.Employer?.Name ?? "");
                context.SetVariable("ResumePath", _options.DefaultResumePath ?? "");
                context.SetVariable("CoverLetterPath", _options.DefaultCoverLetterPath ?? "");
                context.SetVariable("UserFirstName", "Test");
                context.SetVariable("UserSecondName", "Test 2");
                context.SetVariable("UserPhone", "793 793 793");

                await _workflowExecutor.ExecuteWorkflowAsync(workflow, context);

                bool isApplied = false;
                
                if (context.TryGetVariable("WorkflowFinishedSuccessfully", out bool? successValue))
                {
                    isApplied = successValue ?? false;
                }
                //Mark the job as applied todo
                jobOffer.IsApplied = isApplied;
                jobOffer.ShouldApply = !isApplied;

                jobOffer.ApplicationAttempts.Add(new ApplicationAttempt
                {
                    AppliedAt = DateTime.UtcNow,
                    Status = isApplied ? "Success" : "Failure"
                });

                await dbContext.SaveChangesAsync(stoppingToken);

                if (isApplied)
                {
                    _logger.LogInformation("Successfully applied to job: {Url}", jobOffer.Url);
                }
                else
                {
                    _logger.LogWarning("Failed to apply to job: {Url}", jobOffer.Url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying to job: {Url}", jobOffer.Url);
            }
        }

        private string DetermineWorkflowName(string url)
        {
            var domain = new Uri(url).Host;

            return domain switch
            {
                "www.pracuj.pl" => "pracuj",
                "linkedin.com" => "linkedin",
                "indeed.com" => "indeed",
                _ => throw new ArgumentException($"No workflow defined for domain: {domain}"),
            };
        }
    }
}