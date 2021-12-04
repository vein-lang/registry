namespace core.services.searchs.models;

using Newtonsoft.Json;

/// <summary>
/// A package that depends on the queried package.
/// </summary>
public class DependentResult
{
    /// <summary>
    /// The dependent package id.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// The description of the dependent package.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    /// The total downloads for the dependent package.
    /// </summary>
    [JsonProperty("totalDownloads")]
    public long TotalDownloads { get; set; }
}