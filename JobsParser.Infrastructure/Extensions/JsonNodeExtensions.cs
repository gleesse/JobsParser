using System.Text.Json.Nodes;

namespace JobsParser.Infrastructure.Extensions
{
    public static class JsonNodeExtensions
    {
        public static JsonNode GetByPath(this JsonNode node, string path)
        {
            if (node == null || string.IsNullOrEmpty(path))
                return null;

            var segments = path.Split('.');
            var currentNode = node;

            foreach (var segment in segments)
            {
                currentNode = currentNode?[segment];
                if (currentNode == null)
                    return null;
            }

            return currentNode;
        }
    }
}
