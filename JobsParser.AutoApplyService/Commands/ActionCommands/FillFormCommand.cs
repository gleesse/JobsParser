using JobsParser.AutoApplyService.Models;
using JobsParser.AutoApplyService.Repositories;
using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class FillFormCommand(string formId, IFormRepository formRepository, ILogger<FillFormCommand> logger) : Command
    {
        private readonly string _formId = formId ?? throw new ArgumentNullException(nameof(formId));
        private readonly IFormRepository _formRepository = formRepository ?? throw new ArgumentNullException(nameof(formRepository));
        private readonly ILogger<FillFormCommand> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedFormId = ResolveVariables(_formId, context);

            var formConfig = await _formRepository.GetFormConfigurationAsync(resolvedFormId);

            await FillFormAsync(page, context, formConfig);
        }

        private async Task FillFormAsync(IPage page, CommandContext context, FormConfiguration formConfig)
        {
            ArgumentNullException.ThrowIfNull(formConfig);

            if (formConfig.Fields == null || formConfig.Fields.Count == 0)
            {
                _logger.LogWarning($"Form configuration {formConfig.FormId} has no fields to fill");
                return;
            }

            _logger.LogInformation($"Filling form {formConfig.FormName} (ID: {formConfig.FormId}) with {formConfig.Fields.Count} fields");

            foreach (var field in formConfig.Fields)
            {
                bool success = await FillFieldAsync(page, context, field);

                if (success)
                {
                    _logger.LogDebug($"Successfully filled field {field.FieldName}");
                }
                else
                {
                    _logger.LogWarning($"Failed to fill field {field.FieldName}");
                }
            }
        }

        private async Task<bool> FillFieldAsync(IPage page, CommandContext context, FormFieldConfig field)
        {
            try
            {
                var resolvedSelector = ResolveVariables(field.Selector, context);

                var element = page.Locator(resolvedSelector);

                var count = await element.CountAsync();

                if (count > 0)
                {
                    var isVisible = await element.IsVisibleAsync();
                    var isEnabled = await element.IsEnabledAsync();

                    if (isVisible && isEnabled)
                    {
                        string resolvedValue = ResolveVariables(field.DataValue, context);

                        await FillElementByType(page, resolvedSelector, resolvedValue, field.FieldType);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error filling field {field.FieldName}");
                return false;
            }
        }

        private async Task FillElementByType(IPage page, string selector, string value, string fieldType)
        {
            switch (fieldType?.ToLowerInvariant())
            {
                case "text":
                case "email":
                case "password":
                case "textarea":
                    await page.FillAsync(selector, "");
                    await page.FillAsync(selector, value);
                    break;

                case "select":
                    await page.SelectOptionAsync(selector, new[] { value });
                    break;

                case "radio":
                    string radioSelector = $"{selector}[value='{value}']";
                    await page.CheckAsync(radioSelector);
                    break;

                case "checkbox":
                    var checkValue = bool.Parse(value);
                    if (checkValue)
                    {
                        await page.CheckAsync(selector);
                    }
                    else
                    {
                        await page.UncheckAsync(selector);
                    }
                    break;

                case "file":
                    try
                    {
                        await page.SetInputFilesAsync(selector, value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to upload file {value} to {selector}");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported field type {fieldType}");
            }
        }
    }
}