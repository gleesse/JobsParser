namespace JobsParser.Core.Models
{
    public class ContractDetails
    {
        public Guid Id { get; set; }
        public string TypeOfContract { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public string Currency { get; set; }
        public string TimeUnit { get; set; }
        public Guid JobOfferId { get; set; }
        public OfferDto JobOffer { get; set; }
    }
}
