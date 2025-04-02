using JobsParser.AutoApplyService.Commands;

namespace JobsParser.AutoApplyService.DSL
{
    public interface IJsonDslInterpreter
    {
        public Command ParseWorkflow(string json);
    }
}
