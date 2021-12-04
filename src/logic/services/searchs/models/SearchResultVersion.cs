namespace core.services.searchs.models;

using Newtonsoft.Json;

/// <summary>
/// A single version from a <see cref="SearchResult"/>.
/// </summary>
public class SearchResultVersion
{
    /// <summary>
    /// The registration leaf URL for this single version of the matched package.
    /// </summary>
    [JsonProperty("@id")]
    public string RegistrationLeafUrl { get; set; }

    /// <summary>
    /// The package's full NuGet version after normalization, including any SemVer 2.0.0 build metadata.
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; }

    /// <summary>
    /// The downloads for this single version of the matched package.
    /// </summary>
    [JsonProperty("downloads")]
    public ulong Downloads { get; set; }
}
