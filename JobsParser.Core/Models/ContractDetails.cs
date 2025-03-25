namespace JobsParser.Core.Models
{
    public class ContractDetails
    {
        public int Id { get; set; }
        public string TypeOfContract { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public string Currency { get; set; }
        public string TimeUnit { get; set; }
        public int JobOfferId { get; set; }
        public OfferDto JobOffer { get; set; }
    }
}
