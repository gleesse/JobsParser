namespace JobsParser.Core.Models
{
    public class ApplicationAttempt
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string ErrorMsg { get; set; }
        public DateTime? AppliedAt { get; set; }
        public OfferDto Offer { get; set; }
    }
}
