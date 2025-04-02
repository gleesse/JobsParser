using JobsParser.AutoApplyService.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JobsParser.AutoApplyService.Repositories
{
    public class FormRepository(IOptions<AutoApplyServiceOptions> options) : IFormRepository
    {
        private readonly string _formsDirectory = options.Value.FormsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Forms");
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task<FormConfiguration> GetFormConfigurationAsync(string formId)
        {
            if (string.IsNullOrEmpty(formId))
            {
                throw new ArgumentNullException(nameof(formId));
            }

            string filePath = Path.Combine(_formsDirectory, $"{formId}.json");

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"Form configuration file {filePath} not found for form ID {formId}");
            }

            string jsonContent = await File.ReadAllTextAsync(filePath);

            return JsonSerializer.Deserialize<FormConfiguration>(jsonContent, _jsonOptions) ?? throw new InvalidOperationException("Deserialization failed");
        }
    }
}