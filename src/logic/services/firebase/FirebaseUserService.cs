namespace core.services;

using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using aspects;
using Flurl.Http;
using Google.Cloud.Firestore;
using searchs;

public class FirebaseUserService : IUserService, ILoggerAccessor
{
    private readonly IHttpContextAccessor _ctx;
    private readonly IPackageService _packageService;
    private readonly FirestoreDb _operationBuilder;
    private readonly ILogger<FirebaseUserService> _logger;
    private readonly IConfiguration _config;

    public FirebaseUserService(
        IHttpContextAccessor ctx,
        IPackageService packageService,
        FirestoreDb operationBuilder,
        ILogger<FirebaseUserService> logger,
        IConfiguration config)
    {
        _ctx = ctx;
        _packageService = packageService;
        _operationBuilder = operationBuilder;
        _logger = logger;
        _config = config;
    }
    [Interceptor("Failed generate api key by '{0}' name. [eol: {1}]")]
    public async ValueTask<ApiKey> GenerateApiKeyAsync(string name, TimeSpan endOfLife)
    {
        var me = await GetMeAsync();
        var apiKey = new ApiKey();

        apiKey.CreationDate = DateTimeOffset.Now;
        apiKey.EndOfLife = endOfLife;
        apiKey.Name = name;
        apiKey.UserOwner = me.UID;
        apiKey.UID = $"{Guid.NewGuid()}";

        await _operationBuilder.Collection("apiKeys")
            .Document($"{apiKey.UID}")
            .CreateAsync(apiKey);

        return apiKey;
    }

    [Interceptor("Failed get api keys.")]
    public async Task<IReadOnlyCollection<ApiKey>> GetApiKeysAsync()
    {
        var me = await GetMeAsync();

        var list = await _operationBuilder.Collection("apiKeys")
            .ListDocumentsAsync()
            .SelectAwait(async x => await x.GetSnapshotAsync())
            .Where(x => x.GetValue<string>("owner").Equals(me.UID))
            .ToListAsync();

        return list.Select(x => x.ConvertTo<ApiKey>()).ToList().AsReadOnly();
    }

    [Interceptor("Failed remove api key.")]
    public async Task DeleteApiKeyAsync(string uid)
    {
        var me = await GetMeAsync();

        await _operationBuilder.Collection("apiKeys")
            .Document(uid)
            .DeleteAsync();
    }

    [Interceptor("Failed get current user.")]
    public async ValueTask<RegistryUser?> GetMeAsync(CancellationToken token = default)
    {
        var subKey = _ctx.HttpContext!.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        var apiKey = (string)_ctx.HttpContext.Request.Headers["X-VEIN-API-KEY"];

        if (apiKey is not null)
        {
            var keyData = await _operationBuilder.Collection("apiKeys")
                .ListDocumentsAsync()
                .FirstOrDefaultAsync(x => x.Id.Equals(apiKey));

            if (keyData is null)
                return null;
            var val = (await keyData.GetSnapshotAsync()).ConvertTo<ApiKey>();
            return (await _operationBuilder
                    .Collection("users")
                    .Document($"{val.UserOwner}")
                    .GetSnapshotAsync(token))
                .ConvertTo<RegistryUser>();
        }

        if (subKey is null)
            throw new AuthenticationException("Sub key is not preset");

        var result = await _operationBuilder
            .Collection("users-refs")
            .Document(subKey.Value)
            .GetSnapshotAsync(token);

        if (result.Exists)
        {
            var link = result.ConvertTo<UserLink>();

            if (link.RequestReIndexing)
            {
                // TODO
            }
            return (await _operationBuilder
                .Collection("users")
                .Document($"{link.InternalUID}")
                .GetSnapshotAsync(token))
                .ConvertTo<RegistryUser>();
        }
        
        
        var userinfo = await $"{_config["Auth0:Authority"]}/userinfo"
            .WithHeader("Authorization", _ctx.HttpContext!.Request.Headers["Authorization"])
            .GetJsonAsync<RegistryUser>(token);


        userinfo.UID = $"{Guid.NewGuid()}";


        var userLink = new UserLink() { InternalUID = userinfo.UID, Sub = userinfo.Sub };

        await _operationBuilder
            .Collection("users-refs")
            .Document(userLink.Sub)
            .CreateAsync(userLink, token);

        await _operationBuilder
            .Collection("users")
            .Document($"{userLink.InternalUID}")
            .CreateAsync(userinfo, token);

        return userinfo;
    }

    [Interceptor("Failed get packages by currently user.")]
    public async ValueTask<IReadOnlyCollection<Package>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        var me = await GetMeAsync(cancellationToken);

        return await _packageService.FindForUserAsync(me.UID, cancellationToken);
    }

    [Interceptor("Failed index package.")]
    public ValueTask IndexPackageAsync(Package package) => throw new NotImplementedException();
    ILogger ILoggerAccessor.GetLogger() => _logger;
}


public interface IUserService
{
    public Task<IReadOnlyCollection<ApiKey>> GetApiKeysAsync();

    public ValueTask<ApiKey> GenerateApiKeyAsync(string name, TimeSpan endOfLife);

    public Task DeleteApiKeyAsync(string uid);

    public ValueTask<RegistryUser> GetMeAsync(CancellationToken token = default);

    public ValueTask<IReadOnlyCollection<Package>> GetPackagesAsync(CancellationToken cancellationToken = default);

    public ValueTask IndexPackageAsync(Package package);
}
