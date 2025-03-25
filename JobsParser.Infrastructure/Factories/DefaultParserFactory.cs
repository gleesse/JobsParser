using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Parsers.Detail;
using JobsParser.Infrastructure.Parsers.Link;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JobsParser.Infrastructure.Factories
{
    public class DefaultParserFactory(IServiceProvider serviceProvider) : IParserFactory
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ConcurrentDictionary<string, IOfferLinkParser> _cachedLinkParsers = new();
        private readonly ConcurrentDictionary<string, IOfferDetailParser> _cachedDetailParsers = new();

        public IOfferLinkParser GetLinkParser(LinkParserOptions options)
        {
            return _cachedLinkParsers.GetOrAdd(options.Type, type =>
            {
                switch (type)
                {
                    case "pagination":
                        var logger = _serviceProvider.GetRequiredService<ILogger<PaginationJobOfferLinkParser>>();
                        return new PaginationJobOfferLinkParser(logger);
                    default:
                        throw new ArgumentException($"Such Type is not configured: {options.Type}");
                }
            });
        }

        public IOfferDetailParser GetDetailParser(DetailParserOptions options)
        {
            return _cachedDetailParsers.GetOrAdd(options.Type, type =>
            {
                switch (type)
                {
                    case "pracuj":
                        var logger = _serviceProvider.GetRequiredService<ILogger<PracujDetailParser>>();
                        var httpWrapper = _serviceProvider.GetRequiredService<IHttpClientWrapper>();
                        return new PracujDetailParser(httpWrapper, logger);
                    default:
                        throw new ArgumentException($"Such Type is not configured: {options.Type}");
                }
            }); ;
        }
    }
}