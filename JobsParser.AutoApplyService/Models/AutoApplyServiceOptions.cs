namespace JobsParser.AutoApplyService.Models
{
    public class AutoApplyServiceOptions
    {
        public int PollingIntervalSeconds { get; set; } = 60;
        public string? WorkflowsDirectory { get; set; }
        public string? FormsDirectory { get; set; }
        public int MaxConcurrentInstances { get; set; } = 2;
        public string? DefaultResumePath { get; set; }
        public string? DefaultCoverLetterPath { get; set; }
        public int ActionDelayMs { get; set; } = 500;
        public int DefaultTimeoutMs { get; set; } = 30000;
        public bool AutoProcessNewLinks { get; set; } = true;
    }
}