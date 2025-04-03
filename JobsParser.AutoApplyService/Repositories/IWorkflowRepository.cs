using JobsParser.AutoApplyService.Commands;

namespace JobsParser.AutoApplyService.Repositories
{
    public interface IWorkflowRepository
    {
        public Task<Command> GetWorkflowAsync(string workflowName);
    }
}
