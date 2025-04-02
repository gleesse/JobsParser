namespace JobsParser.AutoApplyService.Models
{
    public class PlaywrightOptions
    {
        public string? CookiesDirectory { get; set; } = "Cookies";
        public bool UseSavedCookies { get; set; } = true;
        public string? UserAgent { get; set; }
        public string? BrowserChannel { get; set; }
        public bool Headless { get; set; } = true;
    }
}
