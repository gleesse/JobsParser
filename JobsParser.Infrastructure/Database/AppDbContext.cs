using JobsParser.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobsParser.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<OfferDto> Offers { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
                entity.Property(e => e.OfferUrl);
                entity.Property(e => e.ApplicationUrl);
                entity.Property(e => e.IsActive);
                entity.Property(e => e.OneClickApply);
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.UpdatedAt);
                entity.Property(e => e.ValidUntil);
                entity.Property(e => e.SourceOfferId);
                entity.Property(e => e.Title);
                entity.Property(e => e.Description);
                entity.Property(e => e.EmployerId);
                entity.Property(e => e.Employer);
                entity.Property(e => e.Location);
                entity.Property(e => e.AboutUs);
                entity.Property(e => e.Responsibilities);
                entity.Property(e => e.Requirements);

                entity.HasMany(e => e.WorkModes)
                    .WithOne(w => w.JobOffer)
                    .HasForeignKey(w => w.JobOfferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.PositionLevels)
                    .WithOne(p => p.JobOffer)
                    .HasForeignKey(p => p.JobOfferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Technologies)
                    .WithOne(t => t.JobOffer)
                    .HasForeignKey(t => t.JobOfferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ContractDetails)
                    .WithOne(t => t.JobOffer)
                    .HasForeignKey(t => t.JobOfferId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            modelBuilder.Entity<ContractDetails>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TypeOfContract);
                entity.Property(e => e.MinSalary);
                entity.Property(e => e.MaxSalary);
                entity.Property(e => e.Currency);
                entity.Property(e => e.TimeUnit);
            });
        }
    }
}
