namespace JobsParser.Core.Models
{
    public class Employer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsBlacklisted { get; set; } = false;
        public ICollection<OfferDto> Offers { get; set; } = [];
    }
}
