namespace JobsParser.Core.Abstractions
{
    public interface IQueueService
    {
        Task PublishAsync<T>(string queueName, T message);
        Task ConsumeAsync<T>(string queueName, Func<T, Task> handler);
    }
}
