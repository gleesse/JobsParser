namespace JobsParser.Core.Models
{
    public class OfferDto
    {
        public int Id { get; set; }
        public DateTime? AudDate { get; set; } = DateTime.Now;
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Requirements { get; set; }
        public string? Responsibilities { get; set; }
        public string? Location { get; set; }
        public bool IsApplied { get; set; }
        public bool ShouldApply { get; set; }
        public Employer Employer { get; set; }
        public ContractDetails? ContractDetails { get; set; }
        public WorkMode WorkMode { get; set; }
        public PositionLevel PositionLevel { get; set; }
        public ICollection<Technology> Technologies { get; set; } = [];
        public ICollection<ApplicationAttempt> ApplicationAttempts { get; set; } = [];
    }
}
