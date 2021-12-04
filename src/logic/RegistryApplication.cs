namespace core;

public class RegistryApplication
{
    public RegistryApplication(IServiceCollection services)
        => Services = services ?? throw new ArgumentNullException(nameof(services));

    public IServiceCollection Services { get; }
}