namespace core.services;

using Microsoft.Extensions.Options;

public class ApiKeyAuthenticationService : IAuthenticationService
{
    private readonly string? _apiKey;

    public ApiKeyAuthenticationService(IOptionsSnapshot<RegistryOptions> options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        _apiKey = string.IsNullOrEmpty(options.Value.ApiKey) ? null : options.Value.ApiKey;
    }

    public Task<bool> AuthenticateAsync(string apiKey, CancellationToken cancellationToken)
        => Task.FromResult(Authenticate(apiKey));

    private bool Authenticate(string apiKey)
    {
        // No authentication is necessary if there is no required API key.
        if (_apiKey == null) return true;

        return _apiKey == apiKey;
    }
}


public interface IAuthenticationService
{
    Task<bool> AuthenticateAsync(string apiKey, CancellationToken cancellationToken);
}
