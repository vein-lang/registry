namespace core.services.searchs.models;

using Newtonsoft.Json;

/// <summary>
/// A package that matched a search query.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The ID of the matched package.
    /// </summary>
    [JsonProperty("id")]
    public string PackageId { get; set; }

    /// <summary>
    /// The latest version of the matched pacakge. This is the full NuGet version after normalization,
    /// including any SemVer 2.0.0 build metadata.
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; }

    /// <summary>
    /// The description of the matched package.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    /// The authors of the matched package.
    /// </summary>
    [JsonProperty("authors")]
    public IReadOnlyList<string> Authors { get; set; }

    /// <summary>
    /// The URL of the matched package's icon.
    /// </summary>
    [JsonProperty("iconUrl")]
    public string IconUrl { get; set; }

    /// <summary>
    /// The URL of the matched package's license.
    /// </summary>
    [JsonProperty("licenseUrl")]
    public string LicenseUrl { get; set; }

    /// <summary>
    /// The package types defined by the package author.
    /// </summary>
    [JsonProperty("packageTypes")]
    public IReadOnlyList<SearchResultPackageType> PackageTypes { get; set; }

    /// <summary>
    /// The URL of the matched package's homepage.
    /// </summary>
    [JsonProperty("projectUrl")]
    public string? ProjectUrl { get; set; }

    /// <summary>
    /// The URL for the matched package's registration index.
    /// </summary>
    [JsonProperty("registration")]
    public string RegistrationIndexUrl { get; set; }

    /// <summary>
    /// The summary of the matched package.
    /// </summary>
    [JsonProperty("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// The tags of the matched package.
    /// </summary>
    [JsonProperty("tags")]
    public IReadOnlyList<string> Tags { get; set; }

    /// <summary>
    /// The title of the matched package.
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; }

    /// <summary>
    /// The total downloads for all versions of the matched package.
    /// </summary>
    [JsonProperty("totalDownloads")]
    public ulong TotalDownloads { get; set; }

    /// <summary>
    /// The versions of the matched package.
    /// </summary>
    [JsonProperty("versions")]
    public IReadOnlyList<SearchResultVersion> Versions { get; set; }
}
