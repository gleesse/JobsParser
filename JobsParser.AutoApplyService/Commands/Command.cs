using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace JobsParser.AutoApplyService.Commands
{
    public abstract class Command
    {
        public abstract Task ExecuteAsync(IPage page, CommandContext context);

        protected string ResolveVariables(string input, CommandContext context)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains("${"))
            {
                return input;
            }

            string result = input;
            foreach (Match match in Regex.Matches(input, @"\$\{([^}]+)\}"))
            {
                var variableName = match.Groups[1].Value;

                if (context.TryGetVariable(variableName, out string? variableValue) && !string.IsNullOrEmpty(variableValue))
                {
                    result = result.Replace($"${{{variableName}}}", variableValue);
                }
            }

            return result;
        }
    }
}