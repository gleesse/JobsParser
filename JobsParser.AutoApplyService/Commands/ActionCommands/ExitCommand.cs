using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ExitCommand(bool success, string? message, ILogger<ExitCommand> logger) : Command(logger)
    {
        public override Task ExecuteAsync(IPage page, CommandContext context)
        {
            _logger.LogInformation("Executing exit command with success: {Success}", success);
            context.SetVariable("WorkflowFinishedSuccessfully", success);

            if(!string.IsNullOrEmpty(message))
                context.SetVariable("WorkflowFinishedMessage", message);

            return Task.CompletedTask;
        }
    }
}