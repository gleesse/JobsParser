using JobsParser.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobsParser.Infrastructure.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<OfferDto> Offers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureJobOffer(modelBuilder);
        }

        private void ConfigureJobOffer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OfferDto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AudDate);
                entity.Property(e => e.Url);
                entity.Property(e => e.Title);
                entity.Property(e => e.Description);
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
