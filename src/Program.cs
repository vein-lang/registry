using core;
using core.services;
using core.services.searchs;
using Newtonsoft.Json;


var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddTransient<IValidateOptions<RegistryOptions>, ConfigureRegistryOptions>();

builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);


builder.Services
    .AddControllers()
    .AddNewtonsoftJson(x =>
    {
        x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        x.SerializerSettings.Formatting = Formatting.Indented;
    });

builder.Services
    .AddRegistryApplication(x => { })
    .AddHttpContextAccessor()
    .AddHealthChecks();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();
}

app.UseOperationCancelledMiddleware();
app.UseRouting();
app.UseAuthorization();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
    endpoints.MapControllers();
});

app.Run();
