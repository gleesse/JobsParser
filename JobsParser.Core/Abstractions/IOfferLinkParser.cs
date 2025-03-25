using JobsParser.Core.Models;

namespace JobsParser.Core.Abstractions
{
    public interface IOfferLinkParser
    {
        IEnumerable<OfferLinkDto> ParseOfferLinksFromWebsite(WebsiteConfiguration website);
    }
}
