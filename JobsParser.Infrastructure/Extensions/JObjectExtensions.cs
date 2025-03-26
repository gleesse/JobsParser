using Newtonsoft.Json.Linq;

namespace JobsParser.Infrastructure.Extensions
{
    public static class JObjectExtensions
    {
        public static string GetValueByJsonPath(this JObject jsonObject, string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                return null;

            var token = jsonObject.SelectToken(jsonPath);
            return token?.ToString();
        }

        public static List<string> GetArrayByJsonPath(this JObject jsonObject, string jsonPath)
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
    }
}
