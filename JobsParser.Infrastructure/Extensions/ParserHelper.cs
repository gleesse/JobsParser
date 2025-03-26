using HtmlAgilityPack;
using JobsParser.Core.Models;

namespace JobsParser.Infrastructure.Extensions
{
    public static class ParserHelper
    {
        public static Employer CreateEmployer(string name)
        {
            return !string.IsNullOrEmpty(name)
                ? new Employer { Name = name }
                : null;
        }

        public static WorkMode CreateWorkMode(string name)
        {
            return !string.IsNullOrEmpty(name)
                ? new WorkMode { Name = name }
                : null;
        }

        public static PositionLevel CreatePositionLevel(string name)
        {
            return !string.IsNullOrEmpty(name)
                ? new PositionLevel { Name = name }
                : null;
        }

        public static List<Technology> CreateTechnologies(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
                return new List<Technology>();

            return names
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new Technology { Name = name })
                .ToList();
        }

        public static ContractDetails CreateContractDetails(
            string contractType,
            decimal? minSalary,
            decimal? maxSalary,
            string currency,
            string timeUnit)
        {
            if (string.IsNullOrEmpty(contractType))
                return null;

            return new ContractDetails
            {
                TypeOfContract = contractType,
                MinSalary = minSalary ?? 0,
                MaxSalary = maxSalary ?? 0,
                Currency = currency,
                TimeUnit = timeUnit
            };
        }
    }
}