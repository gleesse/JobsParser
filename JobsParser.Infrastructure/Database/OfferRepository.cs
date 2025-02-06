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

        public async Task SaveOfferAsync(OfferDto details, CancellationToken cancellationToken = default)
        {
            _dbContext.Offers.Add(details);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<OfferDto>> GetOffersReadyForSubmissionAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Offers.ToListAsync(cancellationToken);
        }
    }
}
