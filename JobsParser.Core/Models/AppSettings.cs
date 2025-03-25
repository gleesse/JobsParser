namespace JobsParser.Core.Models
{
    public class AppSettings
    {
        public DatabaseSettings DatabaseSettings { get; set; }
        public RabbitSettings RabbitSettings { get; set; }
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public int Timeout { get; set; }
    }

    public class RabbitSettings
    {
        public string HostName { get; set; }
    }

    public class WebsiteConfiguration
    {
        public string Name { get; set; }
        public string SiteUrl { get; set; }
        public IEnumerable<string> SearchUrls { get; set; }
        public LinkParserOptions LinkParserOptions { get; set; }
        public DetailParserOptions DetailParserOptions { get; set; }
    }

    public class LinkParserOptions
    {
        public string Type { get; set; }
        public string ItemSelector { get; set; }
        public string NextPageButtonSelector { get; set; }
        public string ClickWhenLoadedSelector { get; set; }
    }

    public class DetailParserOptions
    {
        public string Type { get; set; }
        public string ItemSelector { get; set; }
        public string NextPageSelector { get; set; }
    }
}