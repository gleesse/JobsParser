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
                "exit" => ParseExitCommand(node),
                "screenshot" => ParseTakeScreenshotCommand(node),
                _ => throw new ArgumentException($"Unknown command type: {type}")
            };
        }

        #region Commands
        private SequenceCommand ParseSequenceCommand(JsonNode node)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SequenceCommand>>();
            var sequenceCommand = new SequenceCommand(logger);
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

            var logger = serviceProvider.GetRequiredService<ILogger<IfElseCommand>>();
            return new IfElseCommand(condition, thenCommand, elseCommand, logger);
        }

        private ClickCommand ParseClickCommand(JsonNode node)
        {
            var selector = node["selector"]?.GetValue<string>();
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentException("Click command requires a selector");
            }

            int? customTimeout = int.TryParse(node["customTimeout"]?.GetValue<string>(), out var parsedCustomTimeout) ? parsedCustomTimeout : null;
            int? waitForTimeoutSeconds = int.TryParse(node["waitForTimeoutSeconds"]?.GetValue<string>(), out var parsedWaitTimeout) ? parsedWaitTimeout : null;
            var waitForNetworkIdle = node["waitForNetworkIdle"]?.GetValue<bool>() ?? false;
            string? waitForSelector = node["waitForSelector"]?.GetValue<string>();

            var logger = serviceProvider.GetRequiredService<ILogger<ClickCommand>>();
            return new ClickCommand(selector, logger, waitForTimeoutSeconds, customTimeout, waitForNetworkIdle, waitForSelector);
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
            var logger = serviceProvider.GetRequiredService<ILogger<NavigateCommand>>();

            return new NavigateCommand(url, waitUntil, logger);
        }

        private ElementExistsCommand ParseElementExistsCommand(JsonNode node)
        {
            var selector = node["selector"]?.GetValue<string>();
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentException("Exists command requires a selector");
            }

            int? timeout = int.TryParse(node["timeout"]?.GetValue<string>(), out var parsedTimeout) ? parsedTimeout : null;
            var logger = serviceProvider.GetRequiredService<ILogger<ElementExistsCommand>>();

            return new ElementExistsCommand(selector, timeout, logger);
        }

        private ExitCommand ParseExitCommand(JsonNode node)
        {
            var success = node["success"]?.GetValue<bool>();
            var message = node["message"]?.GetValue<string?>();

            if (success == null)
            {
                throw new ArgumentException("Exists command requires a success value");
            }

            var logger = serviceProvider.GetRequiredService<ILogger<ExitCommand>>();
            return new ExitCommand(success.Value, message, logger);
        }

        private TakeScreenshotCommand ParseTakeScreenshotCommand(JsonNode node)
        {
            var screenshotPath = node["path"]?.GetValue<string?>() ?? "logs/${JobId}/";

            if (string.IsNullOrEmpty(screenshotPath))
            {
                throw new ArgumentException("Screenshot command requires 'path' variable");
            }

            var logger = serviceProvider.GetRequiredService<ILogger<TakeScreenshotCommand>>();
            return new TakeScreenshotCommand(screenshotPath, logger);
        }
        #endregion
    }
}