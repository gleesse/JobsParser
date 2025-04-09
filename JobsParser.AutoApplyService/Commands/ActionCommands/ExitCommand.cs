using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ExitCommand(bool success, ILogger<ExitCommand> logger) : Command(logger)
    {
        public override Task ExecuteAsync(IPage page, CommandContext context)
        {
            _logger.LogInformation("Exiting workflow with success: {Success}", success);
            context.SetVariable("WorkflowFinishedSuccessfully", success);
            return Task.CompletedTask;
        }
    }
}