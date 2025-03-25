namespace JobsParser.Core.Models
{
    public class OfferLinkDto
    {
        public Guid Id { get; set; }
        public Uri SourceUrl { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    }
}
