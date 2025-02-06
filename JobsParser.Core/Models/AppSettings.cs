namespace JobsParser.Core.Models
{
    public class AppSettings
    {
        public DatabaseSettings DatabaseSettings { get; set; }
        public RabbitSettings RabbitSettings { get; set; }
        public List<ParserSettings> Parsers { get; set; } = [];
        public Dictionary<string, string> ParserMappings { get; set; } = [];
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

    public class ParserSettings
    {
        public string Name { get; set; }
        public List<string> SearchUrls { get; set; }
    }
}
