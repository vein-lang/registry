namespace core.controllers;

using core.services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class ServiceIndexController
{
    private readonly IServiceIndexService _serviceIndex;

    public ServiceIndexController(IServiceIndexService serviceIndex)
        => _serviceIndex = serviceIndex;

    [HttpGet("@/index.json")]
    public async Task<ServiceIndexResponse> GetAsync(CancellationToken cancellationToken)
        => await _serviceIndex.GetAsync(cancellationToken);
}


/// <summary>
/// The entry point for a NuGet package source used by the client to discover NuGet APIs.
/// </summary>
public class ServiceIndexResponse
{
    /// <summary>
    /// The service index's version.
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; }

    /// <summary>
    /// The service index's full version.
    /// </summary>
    [JsonProperty("full_version")]
    public string FullVersion { get; set; }

    /// <summary>
    /// The resources declared by this service index.
    /// </summary>
    [JsonProperty("resources")]
    public IReadOnlyList<ServiceIndexItem> Resources { get; set; }
}


/// <summary>
/// A resource in the <see cref="ServiceIndexResponse"/>.
/// </summary>
public class ServiceIndexItem
{
    /// <summary>
    /// The resource's base URL.
    /// </summary>
    [JsonProperty("@id")]
    public string ResourceUrl { get; set; }

    /// <summary>
    /// The resource's type.
    /// </summary>
    [JsonProperty("@type")]
    public string Type { get; set; }

    /// <summary>
    /// Human readable comments about the resource.
    /// </summary>
    [JsonProperty("comment")]
    public string Comment { get; set; }
}
