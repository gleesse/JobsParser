namespace JobsParser.Core.Models
{
    public class Technology
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid JobOfferId { get; set; }
        public OfferDto JobOffer { get; set; }
    }
}
