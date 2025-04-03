using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class ExitCommand : Command
    {
        private readonly bool _success;

        public ExitCommand(bool success)
        {
            _success = success;
        }

        public override Task ExecuteAsync(IPage page, CommandContext context)
        {
            context.SetVariable("WorkflowFinishedSuccessfully", _success);
            return Task.CompletedTask;
        }
    }
} 