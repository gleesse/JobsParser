using JobsParser.AutoApplyService.Commands;
using JobsParser.AutoApplyService.DSL;
using JobsParser.AutoApplyService.Models;
using Microsoft.Extensions.Options;

namespace JobsParser.AutoApplyService.Repositories
{
    public class WorkflowRepository(IJsonDslInterpreter interpreter, IOptions<AutoApplyServiceOptions> options) : IWorkflowRepository
    {
        private readonly string _workflowsDirectory = options.Value.WorkflowsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");

        public async Task<Command> GetWorkflowAsync(string workflowName)
        {
            var filePath = Path.Combine(_workflowsDirectory, $"{workflowName}.json");

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"Workflow not found: {workflowName}");
            }

            var workflowJson = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrEmpty(workflowJson))
            {
                throw new ArgumentException($"No workflow found for WorkflowName: {workflowName}");
            }

            return interpreter.ParseWorkflow(workflowJson);
        }
    }
}