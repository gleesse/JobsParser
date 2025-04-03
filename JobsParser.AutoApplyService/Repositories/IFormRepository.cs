using JobsParser.AutoApplyService.Models;

namespace JobsParser.AutoApplyService.Repositories
{
    public interface IFormRepository
    {
        Task<FormConfiguration> GetFormConfigurationAsync(string formId);
    }
}