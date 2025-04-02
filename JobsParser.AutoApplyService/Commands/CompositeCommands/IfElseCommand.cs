using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.CompositeCommands
{
    public class IfElseCommand(Command condition, Command thenCommand, Command? elseCommand = null) : Command
    {
        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            await condition.ExecuteAsync(page, context);
            //todo result should be returned from command execution not from context
            bool conditionResult = context.GetVariable<bool>("ConditionResult");

            if (conditionResult)
            {
                await thenCommand.ExecuteAsync(page, context);
            }
            else if (elseCommand != null)
            {
                await elseCommand.ExecuteAsync(page, context);
            }
        }
    }
}