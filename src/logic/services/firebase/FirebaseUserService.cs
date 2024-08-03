namespace core.services;

using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using aspects;
using FirebaseAdmin.Auth;
using Flurl.Http;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using searchs;

public class FirebaseUserService(
    IHttpContextAccessor ctx,
    IPackageService packageService,
    FirestoreDb operationBuilder,
    ILogger<FirebaseUserService> logger,
    IConfiguration config)
    : IUserService, ILoggerAccessor
{
    private readonly IConfiguration _config = config;

    [Interceptor("Failed generate api key by '{0}' name. [eol: {1}]")]
    public async ValueTask<ApiKey> GenerateApiKeyAsync(string name, TimeSpan endOfLife)
    {
        var me = await GetMeAsync();
        var apiKey = new ApiKey();

        apiKey.CreationDate = DateTimeOffset.Now;
        apiKey.EndOfLife = endOfLife;
        apiKey.Name = name;
        apiKey.UserOwner = me.Uid;
        apiKey.UID = $"{Guid.NewGuid()}";

        await operationBuilder.Collection("apiKeys")
            .Document($"{apiKey.UID}")
            .CreateAsync(apiKey);

        return apiKey;
    }

    [Interceptor("Failed get api keys.")]
    public async Task<IReadOnlyCollection<ApiKey>> GetApiKeysAsync()
    {
        var me = await GetMeAsync();

        var list = await operationBuilder.Collection("apiKeys")
            .ListDocumentsAsync()
            .SelectAwait(async x => await x.GetSnapshotAsync())
            .Where(x => x.GetValue<string>("owner").Equals(me.Uid))
            .ToListAsync();

        return list.Select(x => x.ConvertTo<ApiKey>()).ToList().AsReadOnly();
    }

    [Interceptor("Failed UserAllowedSkipPublishVerification")]
    public async Task<bool> UserAllowedSkipPublishVerification()
    {
        var me = await GetMeAsync();

        var user = await operationBuilder.Collection("users")
            .Document(me.Uid)
            .GetSnapshotAsync();
        if (!user.Exists)
            return false;

        var userData = user.ConvertTo<UserDetails>();

        return userData.IsAllowedSkipPublishVerification;
    }

    [Interceptor("Failed get access details for user")]
    public async Task<bool> UserAllowedPublishWorkloads()
    {
        var me = await GetMeAsync();

        var user = await operationBuilder.Collection("users")
            .Document(me.Uid)
            .GetSnapshotAsync();
        if (!user.Exists)
            return false;

        var userData = user.ConvertTo<UserDetails>();

        return userData.IsAllowedPublishWorkloads;
    }

    [Interceptor("Failed remove api key.")]
    public async Task DeleteApiKeyAsync(string uid)
    {
        var me = await GetMeAsync();

        await operationBuilder.Collection("apiKeys")
            .Document(uid)
            .DeleteAsync();
    }

    [Interceptor("Failed get current user.")]
    public async ValueTask<UserRecord?> GetMeAsync(CancellationToken token = default)
    {
        var subKey = ctx.HttpContext!.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        var apiKey = (string)ctx.HttpContext.Request.Headers["X-VEIN-API-KEY"];

        if (apiKey is not null)
        {
            var keyData = await operationBuilder.Collection("apiKeys")
                .ListDocumentsAsync()
                .FirstOrDefaultAsync(x => x.Id.Equals(apiKey), cancellationToken: token);

            if (keyData is null)
                return null;
            var val = (await keyData.GetSnapshotAsync(token)).ConvertTo<ApiKey>();
            
            return await FirebaseAuth.DefaultInstance.GetUserAsync(val.UserOwner, token);
        }

        if (subKey is null)
            throw new AuthenticationException("Sub key is not preset");
        
        return await FirebaseAuth.DefaultInstance.GetUserAsync(subKey.Value);

        /*var userLink = new UserLink() { InternalUID = userinfo.UID, Sub = userinfo.Sub };

        await _operationBuilder
            .Collection("users-refs")
            .Document(userLink.Sub)
            .CreateAsync(userLink, token);

        await _operationBuilder
            .Collection("users")
            .Document($"{userLink.InternalUID}")
            .CreateAsync(userinfo, token);

        return userinfo;
        
        var userinfo = await $"{_config["Auth0:Authority"]}/userinfo"
            .WithHeader("Authorization", _ctx.HttpContext!.Request.Headers["Authorization"])
            .GetJsonAsync<UserRecord>(token);


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

        return userinfo;*/
    }

    [Interceptor("Failed get packages by currently user.")]
    public async ValueTask<IReadOnlyCollection<Package>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        var me = await GetMeAsync(cancellationToken);

        return await packageService.FindForUserAsync(me.Uid, cancellationToken);
    }

    [Interceptor("Failed index package.")]
    public ValueTask IndexPackageAsync(Package package) => throw new NotImplementedException();
    ILogger ILoggerAccessor.GetLogger() => logger;
}


public interface IUserService
{
    public Task<IReadOnlyCollection<ApiKey>> GetApiKeysAsync();

    public ValueTask<ApiKey> GenerateApiKeyAsync(string name, TimeSpan endOfLife);

    public Task DeleteApiKeyAsync(string uid);

    public ValueTask<UserRecord?> GetMeAsync(CancellationToken token = default);

    public ValueTask<IReadOnlyCollection<Package>> GetPackagesAsync(CancellationToken cancellationToken = default);

    public ValueTask IndexPackageAsync(Package package);

    public Task<bool> UserAllowedPublishWorkloads();
    public Task<bool> UserAllowedSkipPublishVerification();
}
