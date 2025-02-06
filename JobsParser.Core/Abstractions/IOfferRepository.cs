using JobsParser.Core.Models;

namespace JobsParser.Core.Abstractions
{
    public interface IOfferRepository
    {
        Task SaveOfferAsync(OfferDto details, CancellationToken cancellationToken = default);
        Task<IEnumerable<OfferDto>> GetOffersReadyForSubmissionAsync(CancellationToken cancellationToken = default);
    }
}
