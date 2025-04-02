namespace JobsParser.AutoApplyService.Commands.CompositeCommands
{
    public abstract class CompositeCommand : Command
    {
        protected readonly List<Command> Children = [];

        public void AddCommand(Command command)
        {
            Children.Add(command);
        }
    }
}