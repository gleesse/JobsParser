using JobsParser.AutoApplyService.Models;
using JobsParser.AutoApplyService.Repositories;
using Microsoft.Playwright;

namespace JobsParser.AutoApplyService.Commands.ActionCommands
{
    public class FillFormCommand : Command
    {
        private readonly string _formId;
        private readonly IFormRepository _formRepository;

        public FillFormCommand(
            string formId,
            IFormRepository formRepository,
            ILogger<FillFormCommand> logger)
            : base(logger)
        {
            _formId = formId ?? throw new ArgumentNullException(nameof(formId));
            _formRepository = formRepository ?? throw new ArgumentNullException(nameof(formRepository));
        }

        public override async Task ExecuteAsync(IPage page, CommandContext context)
        {
            var resolvedFormId = ResolveVariables(_formId, context);
            _logger.LogInformation("Filling form with ID: {FormId}", resolvedFormId);

            var formConfig = await _formRepository.GetFormConfigurationAsync(resolvedFormId);

            await FillFormAsync(page, context, formConfig);
        }

        private async Task FillFormAsync(IPage page, CommandContext context, FormConfiguration formConfig)
        {
            ArgumentNullException.ThrowIfNull(formConfig);

            if (formConfig.Fields == null || formConfig.Fields.Count == 0)
            {
                _logger.LogWarning("Form configuration {FormId} has no fields to fill", formConfig.FormId);
                return;
            }

            _logger.LogInformation("Filling form {FormName} (ID: {FormId}) with {FieldCount} fields",
                formConfig.FormName, formConfig.FormId, formConfig.Fields.Count);

            foreach (var field in formConfig.Fields)
            {
                bool success = await FillFieldAsync(page, context, field);

                if (success)
                {
                    _logger.LogDebug("Successfully filled field {FieldName}", field.FieldName);
                }
                else
                {
                    _logger.LogWarning("Failed to fill field {FieldName}", field.FieldName);
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

                if (count == 0)
                {
                    _logger.LogWarning("Element with selector '{Selector}' for field '{FieldName}' was not found on the page",
                        resolvedSelector, field.FieldName);
                    return false;
                }

                var isVisible = await element.IsVisibleAsync();
                var isEnabled = await element.IsEnabledAsync();

                if (!isVisible || !isEnabled)
                {
                    _logger.LogWarning("Element with selector '{Selector}' for field '{FieldName}' is not visible or enabled. Visible: {IsVisible}, Enabled: {IsEnabled}",
                        resolvedSelector, field.FieldName, isVisible, isEnabled);
                    return false;
                }

                string resolvedValue = ResolveVariables(field.DataValue, context);
                _logger.LogDebug("Filling field '{FieldName}' with type '{FieldType}' using selector '{Selector}'",
                    field.FieldName, field.FieldType, resolvedSelector);

                await FillElementByType(page, resolvedSelector, resolvedValue, field.FieldType);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error filling field {FieldName}: {ErrorMessage}", field.FieldName, ex.Message);
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
                        _logger.LogError(ex, "Failed to upload file {FileName} to {Selector}: {ErrorMessage}", value, selector, ex.Message);
                    }
                    break;

                default:
                    _logger.LogWarning("Unsupported field type {FieldType}", fieldType);
                    throw new ArgumentException($"Unsupported field type {fieldType}");
            }
        }
    }
}