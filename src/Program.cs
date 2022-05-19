using AspNetCore.Firebase.Authentication.Extensions;
using core;
using core.services;
using core.services.searchs;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;


FirebaseApp.Create(new AppOptions()
{
    ProjectId = "vein-lang",
    Credential = await GoogleCredential.GetApplicationDefaultAsync()
});

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseUrls($"http://*.*.*.*:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");
builder.WebHost.UseSentry(o =>
{
    o.Dsn = Environment.GetEnvironmentVariable("VEIN_SENTRY_DNS") ?? "";
    o.TracesSampleRate = 1.0;
    o.IsGlobalModeEnabled = Environment.GetEnvironmentVariable("VEIN_SENTRY_DNS") is not null;
});

builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);

builder.Services.AddFirebaseAuthentication("https://securetoken.google.com/vein-lang", "vein-lang");

builder.Services
    .AddControllers()
    .AddNewtonsoftJson(x =>
    {
        x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        x.SerializerSettings.Formatting = Formatting.Indented;
        x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });
builder.Services
    .AddRegistryApplication(x => { })
    .AddHttpContextAccessor()
    .AddHealthChecks();


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "localhost",
        builder => builder.WithOrigins(
            "http://localhost:3000",
            "https://localhost:7181")
            .AllowAnyMethod()
            .AllowAnyHeader());
    options.AddPolicy(name: "production",
        builder => builder.WithOrigins(
            "https://api.vein.gallery",
            "https://vein.gallery")
            .AllowAnyHeader()
            .AllowAnyMethod());
});


var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseCors(app.Environment.IsDevelopment() ? "localhost" : "production");
app.UseAuthorization();
app.UseOperationCancelledMiddleware();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
    endpoints.MapControllers();
});

app.Run();
