using HtmlAgilityPack;
using JobsParser.Core.Abstractions;
using JobsParser.Infrastructure.Extensions;

namespace JobsParser.Infrastructure.Extractors
{
    class HtmlValueExtractor : IValueExtractor
    {
        private readonly HtmlDocument _document;

        public HtmlValueExtractor(string htmlContent)
        {
            _document = new HtmlDocument();
            _document.LoadHtml(htmlContent);
        }

        public string ExtractValue(string selector)
        {
            return _document.ExtractText(selector);
        }

        public List<string> ExtractList(string selector)
        {
            return _document.ExtractNodeList(selector);
        }
    }
}
