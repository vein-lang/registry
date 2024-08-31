namespace core;

using controllers;
using core.services;
using core.services.searchs;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using vein.project;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddRegistryApplication(
        this IServiceCollection services,
        Action<RegistryApplication> configureAction)
    {
        var app = new RegistryApplication(services);

        services.AddConfiguration();
        services.AddRegistryServices();
        services.AddDefaultProviders();

        configureAction(app);

        return services;
    }

    private static void AddConfiguration(this IServiceCollection services)
    {
        services.AddAdditionalOptions<RegistryOptions>();
        services.AddAdditionalOptions<DatabaseOptions>(nameof(RegistryOptions.Database));
        services.AddAdditionalOptions<FileSystemStorageOptions>(nameof(RegistryOptions.Storage));
        services.AddAdditionalOptions<SearchOptions>(nameof(RegistryOptions.Search));
        services.AddAdditionalOptions<StorageOptions>(nameof(RegistryOptions.Storage));
    }

    private static void AddRegistryServices(this IServiceCollection services)
    {
        services.AddAutoMapper(x => x.AddMaps(typeof(Mappers)));
        services.AddSingleton(x => new ConverterRegistry
        {
            new PackageAuthorConverter(),
            new GuidConverter(),
            new PackageUrlsConverter(),
            new PackageReferenceConverter()
        });
        services.AddProvider((provider, configuration)
            => new FirestoreDbBuilder
            {
                ConverterRegistry = provider.GetService<ConverterRegistry>(),
                ProjectId = configuration.GetDatabaseConnectionString()
            }.Build());
        services.AddSingleton(GetServiceFromProviders<FirestoreDb>);

        services.TryAddSingleton<NullSearchIndexer>();
        services.TryAddSingleton<NullSearchService>();
        services.TryAddSingleton<NullStorageService>();
        services.TryAddSingleton<NullPackageService>();
        //services.TryAddSingleton<RegistrationBuilder>();
        services.TryAddSingleton<SystemTime>();
        services.TryAddSingleton<ImageDomainValidator>();
        services.TryAddSingleton<ValidateStartupOptions>();
        services.TryAddSingleton<MarkdownService>();
        services.TryAddScoped<IUrlGenerator, RegistryUrlGenerator>();
        services.AddMemoryCache();
        //services.TryAddSingleton(HttpClientFactory);
        //services.TryAddSingleton(NuGetClientFactoryFactory);

        //services.TryAddScoped<DownloadsImporter>();

        services.TryAddTransient<IAuthenticationService, ApiKeyAuthenticationService>();
        services.TryAddTransient<IPackageContentService, PackageContentService>();
        //services.TryAddTransient<IPackageDeletionService, PackageDeletionService>();
        services.TryAddTransient<IPackageIndexingService, PackageIndexingService>();
        //services.TryAddTransient<IPackageMetadataService, DefaultPackageMetadataService>();
        services.TryAddTransient<IPackageStorageService, PackageStorageService>();
        services.TryAddTransient<IServiceIndexService, RegistryServiceIndex>();
        //services.TryAddTransient<ISymbolIndexingService, SymbolIndexingService>();
        //services.TryAddTransient<ISymbolStorageService, SymbolStorageService>();
        services.TryAddScoped<IUserService, FirebaseUserService>();

        services.TryAddScoped<FireOperationBuilder>();
        services.TryAddTransient<FirebasePackageService>();
        services.TryAddTransient<GoogleCloudStorageService>();
        services.TryAddTransient<FirestoreSearchService>();
        services.AddSingleton<PackageCacheSystem>();

    }

    private static void AddDefaultProviders(this IServiceCollection services)
    {
        services.AddProvider<ISearchService>((provider, configuration) =>
        {
            if (configuration.HasSearchType("firestore"))
                return provider.GetRequiredService<FirestoreSearchService>();
            return provider.GetRequiredService<NullSearchService>();
        });
        
        services.AddProvider<IPackageService>((provider, config) =>
        {
            if (config.HasDatabaseType("firestore"))
                return provider.GetRequiredService<FirebasePackageService>();
            return provider.GetRequiredService<NullPackageService>();
        });
        
        services.AddProvider<IStorageService>((provider, configuration) =>
        {
            if (configuration.HasStorageType("google"))
                return provider.GetRequiredService<GoogleCloudStorageService>();
            return provider.GetRequiredService<NullStorageService>();
        });

        services.AddProvider<ISearchIndexer>((provider, config)
            => provider.GetRequiredService<NullSearchIndexer>());
    }

    /// <summary>
    /// Add a new provider to the dependency injection container. The provider may
    /// provide an implementation of the service, or it may return null.
    /// </summary>
    /// <typeparam name="TService">The service that may be provided.</typeparam>
    /// <param name="services">The dependency injection container.</param>
    /// <param name="func">A handler that provides the service, or null.</param>
    /// <returns>The dependency injection container.</returns>
    public static IServiceCollection AddProvider<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, IConfiguration, TService> func)
        where TService : class
    {
        services.AddSingleton<IProvider<TService>>(new DelegateProvider<TService>(func));

        return services;
    }
    /// <summary>
    /// Runs through all providers to resolve the <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service that will be resolved using providers.</typeparam>
    /// <param name="services">The dependency injection container.</param>
    /// <returns>An instance of the service created by the providers.</returns>
    public static TService GetServiceFromProviders<TService>(IServiceProvider services)
        where TService : class
    {
        // Run through all the providers for the type. Find the first provider that results a non-null result.
        var providers = services.GetRequiredService<IEnumerable<IProvider<TService>>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var provider in providers)
        {
            var result = provider.GetOrNull(services, configuration);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public static IServiceCollection AddAdditionalOptions<TOptions>(
        this IServiceCollection services,
        string key = null)
        where TOptions : class
    {
        services.AddSingleton<IValidateOptions<TOptions>>(new ValidateOptions<TOptions>(key));
        services.AddSingleton<IConfigureOptions<TOptions>>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            if (key != null)
            {
                config = config.GetSection(key);
            }

            return new BindOptions<TOptions>(config);
        });

        return services;
    }


    private static readonly string DatabaseTypeKey = $"{nameof(RegistryOptions.Database)}:{nameof(DatabaseOptions.Type)}";
    private static readonly string DatabaseConnectionString
        = $"{nameof(RegistryOptions.Database)}:{nameof(DatabaseOptions.ConnectionString)}";
    private static readonly string SearchTypeKey = $"{nameof(RegistryOptions.Search)}:{nameof(SearchOptions.Type)}";
    private static readonly string StorageTypeKey = $"{nameof(RegistryOptions.Storage)}:{nameof(StorageOptions.Type)}";

    public static string GetDatabaseConnectionString(this IConfiguration config)
        => config[DatabaseConnectionString];

    /// <summary>
    /// Determine whether a database type is currently active.
    /// </summary>
    /// <param name="config">The application's configuration.</param>
    /// <param name="value">The database type that should be checked.</param>
    /// <returns>Whether the database type is active.</returns>
    public static bool HasDatabaseType(this IConfiguration config, string value)
        => config[DatabaseTypeKey].Equals(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determine whether a search type is currently active.
    /// </summary>
    /// <param name="config">The application's configuration.</param>
    /// <param name="value">The search type that should be checked.</param>
    /// <returns>Whether the search type is active.</returns>
    public static bool HasSearchType(this IConfiguration config, string value)
        => config[SearchTypeKey].Equals(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determine whether a storage type is currently active.
    /// </summary>
    /// <param name="config">The application's configuration.</param>
    /// <param name="value">The storage type that should be checked.</param>
    /// <returns>Whether the database type is active.</returns>
    public static bool HasStorageType(this IConfiguration config, string value)
        => config[StorageTypeKey].Equals(value, StringComparison.OrdinalIgnoreCase);
}
