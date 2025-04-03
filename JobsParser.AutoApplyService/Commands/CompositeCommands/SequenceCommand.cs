using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.CompositeCommands
{
    public class SequenceCommand : CompositeCommand
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            foreach (var command in Children)
            {
                await command.ExecuteAsync(page, context);
            }
        }
    }
}