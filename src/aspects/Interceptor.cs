namespace core.aspects;

using MethodDecorator.Fody.Interfaces;
using System.Reflection;
using Newtonsoft.Json;

[AttributeUsage(AttributeTargets.Method)]
public class InterceptorAttribute : Attribute, IMethodDecorator
{
    private readonly string _template;
    private ILoggerAccessor _logger;
    private object _instance;
    private MethodBase _method;
    private object[] _args;

    public InterceptorAttribute(string template) => _template = template;

    public void Init(object instance, MethodBase method, object[] args)
    {
        if (instance is ILoggerAccessor accessor)
        {
            _logger = accessor;
            _instance = instance;
            _args = args;
            _method = method;
        }
    }

    public void OnEntry()
    {
        if (_logger is null) return;
        if (!_logger.GetLogger().IsEnabled(LogLevel.Debug)) return;
        _logger.GetLogger().LogDebug($"Calling '{_method.Name}'. \nargs: {JsonConvert.SerializeObject(_args, Formatting.Indented)}");
    }

    public void OnExit()
    {
        if (_logger is null) return;
        if (!_logger.GetLogger().IsEnabled(LogLevel.Debug)) return;
        _logger.GetLogger().LogDebug($"Complete call '{_method.Name}'.");
    }

    public void OnException(Exception exception)
    {
        if (_logger is null) return;
        _logger.GetLogger().LogError(exception, string.Format(_template, _args));
    }
}


public interface ILoggerAccessor
{
    ILogger GetLogger();
}
