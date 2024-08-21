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

    [JsonProperty("isVerified")]
    public bool IsVerified { get; set; }

    [JsonProperty("hasMetapackage")]
    public bool HasMetapackage { get; set; }

    [JsonProperty("hasServicedPackage")]
    public bool HasServicedPackage { get; set; }


    public const string LatestTag = "latest";
    public const string NextTag = "next";

}
