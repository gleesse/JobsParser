namespace JobsParser.Core.Models
{
    public class DetailParserServiceSettings
    {
        public int MaxAttempts { get; set; }
        public int InitialDelayHours { get; set; }
    }

    public class RabbitSettings
    {
        public string HostName { get; set; }
        public bool EnableRetries { get; set; }
        public int RetryDelayMinutes { get; set; }
        public int MaxRetries { get; set; }
        public string RetryExchange { get; set; }
        public string RetryQueue { get; set; }
        public string FailedQueue { get; set; }
        public string DetailsQueue { get; set; }
        public string LinksQueue { get; set; }
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
        public string JsonScriptSelector { get; set; }
        public string TitleSelector { get; set; }
        public string ResponsibilitiesSelector { get; set; }
        public string RequirementsSelector { get; set; }
        public string LocationSelector { get; set; }
        public string EmployerNameSelector { get; set; }
        public string WorkModeSelector { get; set; }
        public string PositionLevelSelector { get; set; }
        public string TechnologiesSelector { get; set; }
        public string ContractTypeSelector { get; set; }
        public string MinSalarySelector { get; set; }
        public string MaxSalarySelector { get; set; }
        public string CurrencySelector { get; set; }
        public string TimeUnitSelector { get; set; }
    }
}