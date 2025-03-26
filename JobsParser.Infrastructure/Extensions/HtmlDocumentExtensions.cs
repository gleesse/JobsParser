using HtmlAgilityPack;

namespace JobsParser.Infrastructure.Extensions
{
    public static class HtmlDocumentExtensions
    {
        public static string ExtractText(this HtmlDocument doc, string selector)
        {
            if (string.IsNullOrEmpty(selector))
                return null;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.InnerText?.Trim();
        }

        public static string ExtractAttribute(this HtmlDocument doc, string selector, string attributeName)
        {
            if (string.IsNullOrEmpty(selector) || string.IsNullOrEmpty(attributeName))
                return null;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.GetAttributeValue(attributeName, null);
        }

        public static List<string> ExtractNodeList(this HtmlDocument doc, string selector)
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
    }
}
