using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobsParser.Infrastructure.Database
{
    public class OfferRepository : IOfferRepository
    {
        private readonly AppDbContext _dbContext;

        public OfferRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveOfferAsync(OfferDto offer, CancellationToken cancellationToken = default)
        {
            _dbContext.Offers.Add(offer);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<OfferDto>> GetOffersReadyForSubmissionAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Offers.ToListAsync(cancellationToken);
        }

        public async Task<bool> OfferExistsAsync(string url)
        {
            return await _dbContext.Offers.AnyAsync(offer => offer.Url == url);
        }
    }
}
