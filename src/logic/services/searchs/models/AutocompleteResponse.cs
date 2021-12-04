namespace core.services.searchs.models;

using Newtonsoft.Json;

/// <summary>
/// The package ids that matched the autocomplete query.
/// </summary>
public class AutocompleteResponse
{
    [JsonProperty("@context")]
    public AutocompleteContext Context { get; set; }

    /// <summary>
    /// The total number of matches, disregarding skip and take.
    /// </summary>
    [JsonProperty("totalHits")]
    public long TotalHits { get; set; }

    /// <summary>
    /// The package IDs matched by the autocomplete query.
    /// </summary>
    [JsonProperty("data")]
    public IReadOnlyList<string> Data { get; set; }
}