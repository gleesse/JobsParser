using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using JobsParser.Infrastructure.Parsers.Detail;
using JobsParser.Infrastructure.Parsers.Link;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
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
                var page = _serviceProvider.GetRequiredService<IPage>();
                switch (type)
                {
                    case "pagination":
                        var logger = _serviceProvider.GetRequiredService<ILogger<PaginationJobOfferLinkParser>>();
                        return new PaginationJobOfferLinkParser(logger, page);
                    case "infinitescroll":
                        var infiniteScrollLogger = _serviceProvider.GetRequiredService<ILogger<InfiniteScrollJobOfferLinkParser>>();
                        return new InfiniteScrollJobOfferLinkParser(infiniteScrollLogger, page);
                    default:
                        throw new ArgumentException($"Such Type is not configured: {options.Type}");
                }
            });
        }

        public IOfferDetailParser GetDetailParser(DetailParserOptions options)
        {
            return _cachedDetailParsers.GetOrAdd(options.Type, type =>
            {
                var httpClientWrapper = _serviceProvider.GetRequiredService<IHttpClientWrapper>();
                switch (type)
                {
                    //case "json":
                    //    var jsonLogger = _serviceProvider.GetRequiredService<ILogger<JsonDetailParser>>();
                    //    return new JsonDetailParser(httpClientWrapper, jsonLogger, options);
                    //case "html":
                    //    var htmlLogger = _serviceProvider.GetRequiredService<ILogger<HtmlDetailParser>>();
                    //    return new HtmlDetailParser(httpClientWrapper, htmlLogger, options);
                    default:
                        return new DetailParser(httpClientWrapper, _serviceProvider.GetRequiredService<ILogger<DetailParser>>(), options);
                        //throw new ArgumentException($"Such Type is not configured: {options.Type}");
                }
            });
        }
    }
}