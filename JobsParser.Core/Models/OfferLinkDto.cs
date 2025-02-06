namespace JobsParser.Core.Models
{
    public class OfferLinkDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Uri SourceUrl { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    }
}
