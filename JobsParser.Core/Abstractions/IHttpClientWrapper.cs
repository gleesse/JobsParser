namespace JobsParser.Core.Abstractions
{
    public interface IHttpClientWrapper
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default);
    }
}
