namespace JobsParser.AutoApplyService.Commands.CompositeCommands
{
    public abstract class CompositeCommand(ILogger<CompositeCommand> logger) : Command(logger)
    {
        protected readonly List<Command> Children = [];

        public void AddCommand(Command command)
        {
            Children.Add(command);
        }
    }
}