using JobsParser.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobsParser.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<OfferDto> Offers { get; set; }
        public DbSet<Employer> Employers { get; set; }
        public DbSet<WorkMode> WorkModes { get; set; }
        public DbSet<PositionLevel> PositionLevels { get; set; }
        public DbSet<Technology> Technologies { get; set; }
        public DbSet<ContractDetails> ContractDetails { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureJobOffer(modelBuilder);
        }

        #region Repository Methods

        public async Task<bool> OfferExistsAsync(string url)
        {
            return await Offers.AnyAsync(offer => offer.Url == url);
        }

        public async Task<IEnumerable<OfferDto>> GetOffersReadyForSubmissionAsync()
        {
            return await Offers.ToListAsync();
        }

        public async Task SaveOfferAsync(OfferDto offer)
        {
            // Reuse existing related entities to prevent duplication
            if (offer.Employer != null && !string.IsNullOrEmpty(offer.Employer.Name))
                offer.Employer = await Employers.FirstOrDefaultAsync(e => e.Name == offer.Employer.Name) ?? offer.Employer;

            if (offer.WorkMode != null && !string.IsNullOrEmpty(offer.WorkMode.Name))
                offer.WorkMode = await WorkModes.FirstOrDefaultAsync(w => w.Name == offer.WorkMode.Name) ?? offer.WorkMode;

            if (offer.PositionLevel != null && !string.IsNullOrEmpty(offer.PositionLevel.Name))
                offer.PositionLevel = await PositionLevels.FirstOrDefaultAsync(p => p.Name == offer.PositionLevel.Name) ?? offer.PositionLevel;

            if (offer.Technologies != null && offer.Technologies.Any())
            {
                var processedTechnologies = new List<Technology>();

                foreach (var tech in offer.Technologies)
                {
                    if (tech != null && !string.IsNullOrEmpty(tech.Name))
                    {
                        var existingTech = await Technologies.FirstOrDefaultAsync(t => t.Name == tech.Name);
                        processedTechnologies.Add(existingTech ?? tech);
                    }
                }

                offer.Technologies = processedTechnologies;
            }

            Offers.Add(offer);
            await SaveChangesAsync();
        }

        #endregion

        private void ConfigureJobOffer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OfferDto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AudDate);
                entity.Property(e => e.Url);
                entity.Property(e => e.Title);
                entity.Property(e => e.Requirements);
                entity.Property(e => e.Responsibilities);
                entity.Property(e => e.Location);

                entity.HasOne(e => e.Employer)
                    .WithMany(e => e.Offers)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ContractDetails)
                    .WithOne(d => d.JobOffer)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.WorkMode)
                    .WithMany(w => w.Offers)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PositionLevel)
                    .WithMany(p => p.Offers)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Technologies)
                    .WithMany(t => t.Offers);
            });

            modelBuilder.Entity<WorkMode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name);
            });

            modelBuilder.Entity<PositionLevel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name);
            });

            modelBuilder.Entity<Technology>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name);
            });

            modelBuilder.Entity<Employer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name);
                entity.Property(e => e.IsBlacklisted);
            });

            modelBuilder.Entity<ContractDetails>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TypeOfContract);
                entity.Property(e => e.MinSalary).HasPrecision(18, 2);
                entity.Property(e => e.MaxSalary).HasPrecision(18, 2);
                entity.Property(e => e.Currency);
                entity.Property(e => e.TimeUnit);
            });
        }
    }
}
