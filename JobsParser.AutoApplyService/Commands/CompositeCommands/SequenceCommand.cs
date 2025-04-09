using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.CompositeCommands
{
    public class SequenceCommand(ILogger<SequenceCommand> logger) : CompositeCommand(logger)
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            _logger.LogInformation("Executing sequence of {ChildCount} commands", Children.Count);

            foreach (var command in Children)
            {
                await command.ExecuteAsync(page, context);
                
                if (context.TryGetVariable<bool>("WorkflowFinishedSuccessfully", out _))
                {
                    _logger.LogInformation("Exit command was executed, breaking sequence execution");
                    break;
                }
            }

            _logger.LogInformation("Completed sequence execution");
        }
    }
}