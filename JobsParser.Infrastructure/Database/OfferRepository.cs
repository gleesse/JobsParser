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

        public async Task SaveOfferAsync(OfferDto offer)
        {
            _dbContext.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<OfferDto>> GetOffersReadyForSubmissionAsync()
        {
            return await _dbContext.Offers.ToListAsync();
        }

        public async Task<bool> OfferExistsAsync(string url)
        {
            return await _dbContext.Offers.AnyAsync(offer => offer.Url == url);
        }
    }
}