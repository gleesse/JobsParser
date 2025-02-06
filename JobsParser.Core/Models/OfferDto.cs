namespace JobsParser.Core.Models
{
    public class OfferDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Uri OfferUrl { get; set; }
        public string? ApplicationUrl { get; set; }
        public bool? IsActive { get; set; }
        public bool? OneClickApply { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? SourceOfferId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? EmployerId { get; set; }
        public string? Employer { get; set; }
        public string? AboutUs { get; set; }
        public string? Location { get; set; }
        public ICollection<ContractDetails> ContractDetails { get; set; } = new List<ContractDetails>();
        public ICollection<WorkMode> WorkModes { get; set; } = new List<WorkMode>();
        public ICollection<PositionLevel> PositionLevels { get; set; } = new List<PositionLevel>();
        public ICollection<Technology> Technologies { get; set; } = new List<Technology>();
        public string? Responsibilities { get; set; }
        public string? Requirements { get; set; }
    }
}
