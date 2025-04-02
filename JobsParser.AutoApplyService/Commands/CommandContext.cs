using System.Collections.Concurrent;

namespace JobsParser.AutoApplyService.Commands
{
    public class CommandContext
    {
        private readonly ConcurrentDictionary<string, object> _variables = new();

        public T? GetVariable<T>(string key)
        {
            if (_variables.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return default;
        }

        public bool TryGetVariable<T>(string key, out T? value)
        {
            if (_variables.TryGetValue(key, out var objValue) && objValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public void SetVariable<T>(string key, T value)
        {
            _variables[key] = value!;
        }

        public bool RemoveVariable(string key)
        {
            return _variables.TryRemove(key, out _);
        }
    }
}