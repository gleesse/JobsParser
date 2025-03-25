namespace JobsParser.Core.Models
{
    public class PositionLevel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<OfferDto> Offers { get; set; }
    }
}