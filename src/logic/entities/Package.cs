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

    [JsonProperty("hasReadme")]
    public bool HasReadme { get; set; }

    [JsonProperty("normalizedVersion")]
    public string? NormalizedVersionString { get; set; }

    [JsonProperty("originalVersion")]
    public string? OriginalVersionString { get; set; }

    [JsonIgnore]
    public bool HasEmbeddedIcon => Icon?.StartsWith("@/") ?? false;

    [JsonProperty("published")]
    public DateTimeOffset Published { get; set; }
    public static Package CreateFromManifest(PackageManifest manifest) => new Package()
    {
        Downloads = 0,
        HasReadme = false,
        Listed = false,
        NormalizedVersionString = manifest.Version.ToNormalizedString(),
        OriginalVersionString = manifest.Version.OriginalVersion,
        Icon = "https://api.nuget.org/v3-flatcontainer/ivy.library/2.5.9/icon",
        Version = manifest.Version,
        Authors = manifest.Authors,
        Categories = manifest.Categories,
        Dependencies = manifest.Dependencies,
        BugUrl = manifest.BugUrl,
        Description = manifest.Description,
        HomepageUrl = manifest.HomepageUrl,
        IsPreview = manifest.IsPreview,
        Keywords = manifest.Keywords,
        License = manifest.License,
        Name = manifest.Name,
        Repository = manifest.Repository,
        RequireLicenseAcceptance = false
    };
}
