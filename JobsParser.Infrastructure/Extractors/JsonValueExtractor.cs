using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Infrastructure.Extensions;
using Newtonsoft.Json.Linq;

namespace JobsParser.Infrastructure.Extractors
{
    class JsonValueExtractor : IValueExtractor
    {
        private readonly JObject _jsonObject;

        public JsonValueExtractor(string htmlContent, string scriptSelector)
        {
            string jsonString = ExtractJsonFromHtml(htmlContent, scriptSelector);
            _jsonObject = JObject.Parse(jsonString);
        }

        public string ExtractValue(string selector)
        {
            return _jsonObject.GetValueByJsonPath(selector);
        }

        public List<string> ExtractList(string selector)
        {
            return _jsonObject.GetArrayByJsonPath(selector);
        }

        private string ExtractJsonFromHtml(string html, string scriptSelector)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scriptNode = doc.DocumentNode.SelectSingleNode(scriptSelector);
            if (scriptNode == null)
            {
                throw new Exception($"Script node not found using selector: {scriptSelector}");
            }

            string scriptContent = scriptNode.InnerText;

            // Extract the JSON object from the script
            int startIndex = scriptContent.IndexOf("{");
            int endIndex = scriptContent.LastIndexOf("}") + 1;

            if (startIndex < 0 || endIndex <= 0 || endIndex <= startIndex)
            {
                throw new Exception("Valid JSON object not found in script content");
            }

            return scriptContent.Substring(startIndex, endIndex - startIndex);
        }
    }
}
