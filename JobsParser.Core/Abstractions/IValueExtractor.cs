namespace JobsParser.Core.Abstractions
{
    public interface IValueExtractor
    {
        string ExtractValue(string selector);
        List<string> ExtractList(string selector);
    }
}
