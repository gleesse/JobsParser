using JobsParser.AutoApplyService.Commands;
using JobsParser.AutoApplyService.Commands.ActionCommands;
using JobsParser.AutoApplyService.Commands.CompositeCommands;
using JobsParser.AutoApplyService.Repositories;
using System.Text.Json.Nodes;

namespace JobsParser.AutoApplyService.DSL
{
    public class JsonDslInterpreter(IServiceProvider serviceProvider) : IJsonDslInterpreter
    {
        public Command ParseWorkflow(string json)
        {
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode == null)
            {
                throw new ArgumentException("Invalid JSON", nameof(json));
            }

            return ParseCommand(jsonNode);
        }

        private Command ParseCommand(JsonNode node)
        {
            var type = node["type"]?.GetValue<string>();
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException("Command type is required");
            }

            return type.ToLowerInvariant() switch
            {
                "sequence" => ParseSequenceCommand(node),
                "ifelse" => ParseIfElseCommand(node),
                "click" => ParseClickCommand(node),
                "fillform" => ParseFillFormCommand(node),
                "navigate" => ParseNavigateCommand(node),
                "exists" => ParseElementExistsCommand(node),
                _ => throw new ArgumentException($"Unknown command type: {type}")
            };
        }

        #region Commands
        private SequenceCommand ParseSequenceCommand(JsonNode node)
        {
            var sequenceCommand = new SequenceCommand();
            var commands = node["commands"]?.AsArray();

            if (commands != null)
            {
                foreach (var commandNode in commands)
                {
                    if (commandNode != null)
                    {
                        sequenceCommand.AddCommand(ParseCommand(commandNode));
                    }
                }
            }

            return sequenceCommand;
        }

        private IfElseCommand ParseIfElseCommand(JsonNode node)
        {
            var conditionNode = node["condition"];
            if (conditionNode == null)
            {
                throw new ArgumentException("If-else command requires a condition");
            }

            var thenNode = node["then"];
            if (thenNode == null)
            {
                throw new ArgumentException("If-else command requires a then branch");
            }

            var elseNode = node["else"];

            var condition = ParseCommand(conditionNode);
            var thenCommand = ParseCommand(thenNode);
            var elseCommand = elseNode != null ? ParseCommand(elseNode) : null;

            return new IfElseCommand(condition, thenCommand, elseCommand);
        }

        private ClickCommand ParseClickCommand(JsonNode node)
        {
            var selector = node["selector"]?.GetValue<string>();
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentException("Click command requires a selector");
            }

            int? timeout = int.TryParse(node["timeout"]?.GetValue<string>(), out var parsedTimeout) ? parsedTimeout : null;
            var waitForNavigation = node["waitForNavigation"]?.GetValue<bool>() ?? false;

            return new ClickCommand(selector, timeout, waitForNavigation);
        }

        private FillFormCommand ParseFillFormCommand(JsonNode node)
        {
            var formId = node["formId"]?.GetValue<string>();
            if (string.IsNullOrEmpty(formId))
            {
                throw new ArgumentException("Fill command requires a formId");
            }

            var formRepository = serviceProvider.GetRequiredService<IFormRepository>();
            var logger = serviceProvider.GetRequiredService<ILogger<FillFormCommand>>();

            return new FillFormCommand(formId, formRepository, logger);
        }

        private NavigateCommand ParseNavigateCommand(JsonNode node)
        {
            var url = node["url"]?.GetValue<string>();
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Navigate command requires a URL");
            }

            var waitUntil = node["waitUntil"]?.GetValue<string>() ?? "load";

            return new NavigateCommand(url, waitUntil);
        }

        private ElementExistsCommand ParseElementExistsCommand(JsonNode node)
        {
            var selector = node["selector"]?.GetValue<string>();
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentException("Exists command requires a selector");
            }

            int? timeout = int.TryParse(node["timeout"]?.GetValue<string>(), out var parsedTimeout) ? parsedTimeout : null;

            return new ElementExistsCommand(selector, timeout);
        }
        #endregion
    }
}