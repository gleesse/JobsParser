using JobsParser.Core.Models;

namespace JobsParser.Core.Abstractions
{
    public interface IOfferLinkParser
    {
        Task<IEnumerable<OfferLinkDto>> ParseAsync(Uri searchUrl, CancellationToken cancellationToken = default);
    }
}
