using core;
using core.services;
using core.services.searchs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseUrls($"http://*.*.*.*:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

//builder.Services.AddTransient<IValidateOptions<RegistryOptions>, ConfigureRegistryOptions>();

builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Auth0:Authority"];
    options.Audience = builder.Configuration["Auth0:Audience"];
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true
    };
});

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
            "https://registry.vein-lang.org",
            "https://ui.registry.vein-lang.org")
            .AllowAnyHeader()
            .AllowAnyMethod());
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();
}
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
