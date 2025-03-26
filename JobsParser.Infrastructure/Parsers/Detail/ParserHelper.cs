using HtmlAgilityPack;
using JobsParser.Core.Models;
using Newtonsoft.Json.Linq;

namespace JobsParser.Infrastructure.Parsers.Detail
{
    public static class ParserHelper
    {
        #region HTML Parsing Helpers

        public static string ExtractText(HtmlDocument doc, string selector)
        {
            if (string.IsNullOrEmpty(selector))
                return null;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.InnerText?.Trim();
        }

        public static string ExtractAttribute(HtmlDocument doc, string selector, string attributeName)
        {
            if (string.IsNullOrEmpty(selector) || string.IsNullOrEmpty(attributeName))
                return null;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.GetAttributeValue(attributeName, null);
        }

        public static List<string> ExtractNodeList(HtmlDocument doc, string selector)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(selector))
                return result;

            var nodes = doc.DocumentNode.SelectNodes(selector);
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node != null && !string.IsNullOrEmpty(node.InnerText))
                    {
                        result.Add(node.InnerText.Trim());
                    }
                }
            }

            return result;
        }

        public static string ExtractJsonFromHtml(string html, string scriptSelector)
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

        #endregion

        #region JSON Parsing Helpers

        public static string GetValueFromJsonPath(JObject jsonObject, string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                return null;

            var token = jsonObject.SelectToken(jsonPath);
            return token?.ToString();
        }

        public static List<string> GetArrayFromJsonPath(JObject jsonObject, string jsonPath)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(jsonPath))
                return result;

            var tokens = jsonObject.SelectTokens(jsonPath);
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    if (token != null && !string.IsNullOrEmpty(token.ToString()))
                    {
                        result.Add(token.ToString());
                    }
                }
            }

            return result;
        }

        public static decimal? TryParseDecimal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (decimal.TryParse(value, out decimal result))
                return result;

            return null;
        }

        #endregion

        #region Entity Creation Helpers

        public static Employer CreateEmployer(string name)
        {
            return !string.IsNullOrEmpty(name)
                ? new Employer { Name = name }
                : null;
        }

        public static WorkMode CreateWorkMode(string name)
        {
            return !string.IsNullOrEmpty(name)
                ? new WorkMode { Name = name }
                : null;
        }

        public static PositionLevel CreatePositionLevel(string name)
        {
            return !string.IsNullOrEmpty(name)
                ? new PositionLevel { Name = name }
                : null;
        }

        public static List<Technology> CreateTechnologies(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
                return new List<Technology>();

            return names
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new Technology { Name = name })
                .ToList();
        }

        public static ContractDetails CreateContractDetails(
            string contractType,
            decimal? minSalary,
            decimal? maxSalary,
            string currency,
            string timeUnit)
        {
            if (string.IsNullOrEmpty(contractType))
                return null;

            return new ContractDetails
            {
                TypeOfContract = contractType,
                MinSalary = minSalary ?? 0,
                MaxSalary = maxSalary ?? 0,
                Currency = currency,
                TimeUnit = timeUnit
            };
        }

        #endregion
    }
}