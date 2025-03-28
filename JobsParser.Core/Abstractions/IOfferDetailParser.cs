using JobsParser.Core.Models;

namespace JobsParser.Core.Abstractions
{
    public interface IOfferDetailParser
    {
        Task<OfferDto> ParseAsync(Uri offerUrl);
    }
}
