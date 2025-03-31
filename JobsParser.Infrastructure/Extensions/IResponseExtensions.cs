using JobsParser.Infrastructure.Exceptions;
using Microsoft.Playwright;

namespace JobsParser.Infrastructure.Extensions
{
    public static class IResponseExtensions
    {
        public static void EnsureResponseNotRateLimited(this IResponse response)
        {
            if (response?.Status == 429)
            {
                TimeSpan? delay = null;

                if (response.Headers.TryGetValue("Retry-After", out var retryAfterValue))
                {
                    // Try to parse delay - could be in seconds (integer) or as HTTP date
                    if (int.TryParse(retryAfterValue, out int seconds))
                    {
                        delay = TimeSpan.FromSeconds(seconds);
                    }
                    else if (DateTimeOffset.TryParse(retryAfterValue, out DateTimeOffset date))
                    {
                        delay = date - DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        delay = null;
                    }
                }

                throw new HttpTooManyRequestsException("Too many requests sent to the server.", response.Url, delay);
            }
        }
    }
}
