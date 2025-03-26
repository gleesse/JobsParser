using JobsParser.Core.Models;

namespace JobsParser.Core.Abstractions
{
    public interface IOfferRepository
    {
        Task SaveOfferAsync(OfferDto details);
        Task<IEnumerable<OfferDto>> GetOffersReadyForSubmissionAsync();
        Task<bool> OfferExistsAsync(string url);
    }
}
