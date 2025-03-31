namespace JobsParser.Infrastructure.Exceptions
{
    public class HttpTooManyRequestsException(string message, string targetUrl, TimeSpan? delay = null) : HttpRequestException(message)
    {
        public TimeSpan? Delay { get; set; } = delay;
        public string TargetUrl { get; set; } = targetUrl;
    }
}