using JobsParser.Core.Models;

namespace JobsParser.Core.Abstractions
{
    public interface IParserFactory
    {
        IOfferLinkParser GetLinkParser(LinkParserOptions options);
        IOfferDetailParser GetDetailParser(DetailParserOptions options);
    }
}
