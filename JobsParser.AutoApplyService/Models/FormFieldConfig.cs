using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace JobsParser.AutoApplyService.Models
{
    public class FormFieldConfig
    {
        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; }

        [JsonPropertyName("selector")]
        public string Selector { get; set; }

        [JsonPropertyName("dataValue")]
        public string DataValue { get; set; }

        [JsonPropertyName("fieldType")]
        public string FieldType { get; set; }
    }
}