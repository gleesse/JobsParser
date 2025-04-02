using System.Text.Json.Serialization;

namespace JobsParser.AutoApplyService.Models
{
    public class FormConfiguration
    {
        [JsonPropertyName("formId")]
        public string FormId { get; set; }

        [JsonPropertyName("formName")]
        public string FormName { get; set; }

        [JsonPropertyName("fields")]
        public List<FormFieldConfig> Fields { get; set; } = [];
    }
}