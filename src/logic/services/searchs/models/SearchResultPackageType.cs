namespace core.services.searchs.models;

using Newtonsoft.Json;

/// <summary>
/// A single package type from a <see cref="SearchResult"/>.
/// </summary>
public class SearchResultPackageType
{
    /// <summary>
    /// The name of the package type.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
}