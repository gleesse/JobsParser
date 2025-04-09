using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.CompositeCommands
{
    public class IfElseCommand(Command condition, Command thenCommand, Command? elseCommand, ILogger<IfElseCommand> logger) : Command(logger)
    {
        private readonly Command _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        private readonly Command _thenCommand = thenCommand ?? throw new ArgumentNullException(nameof(thenCommand));

        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            _logger.LogInformation("Evaluating condition for if-else statement");

            await _condition.ExecuteAsync(page, context);

            // Result should be returned from command execution not from context
            bool conditionResult = context.GetVariable<bool>("ConditionResult");
            _logger.LogInformation("Condition evaluated to: {Result}", conditionResult);

            if (conditionResult)
            {
                _logger.LogInformation("Executing 'then' branch");
                await _thenCommand.ExecuteAsync(page, context);
            }
            else if (elseCommand != null)
            {
                _logger.LogInformation("Executing 'else' branch");
                await elseCommand.ExecuteAsync(page, context);
            }
            else
            {
                _logger.LogInformation("No 'else' branch to execute");
            }

            _logger.LogInformation("Completed if-else execution");
        }
    }
}