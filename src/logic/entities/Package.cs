namespace core;

using Google.Cloud.Firestore;
using Newtonsoft.Json;
using vein.project;

[FirestoreData]
public record Package : PackageManifest
{
    [JsonProperty("isListed")]
    public bool Listed { get; set; }

    [JsonProperty("downloads")]
    public ulong Downloads { get; set; }
    
    [JsonProperty("normalizedVersion")]
    public string? NormalizedVersionString { get; set; }

    [JsonProperty("originalVersion")]
    public string? OriginalVersionString { get; set; }
    
    [JsonProperty("published")]
    public DateTimeOffset Published { get; set; }
}
