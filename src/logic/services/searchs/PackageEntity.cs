namespace core.services.searchs;

using Google.Cloud.Firestore;
using Newtonsoft.Json;
using vein.project;

[FirestoreData]
public class PackageEntity
{
    [FirestoreProperty]
    public string NormalizedVersion { get; set; }
    [FirestoreProperty]
    public string Id { get; set; }
    [FirestoreProperty]
    public string OriginalVersion { get; set; }
    [FirestoreProperty]
    public List<PackageAuthor> Authors { get; set; }
    [FirestoreProperty]
    public string Description { get; set; }
    [FirestoreProperty]
    public ulong Downloads { get; set; }
    [FirestoreProperty]
    public bool IsPreview { get; set; }
    [FirestoreProperty]
    public bool Listed { get; set; }
    [FirestoreProperty]
    public bool RequireLicenseAcceptance { get; set; }
    [FirestoreProperty]
    public bool HasEmbbededReadme { get; set; }
    [FirestoreProperty]
    public bool HasEmbeddedIcon { get; set; }
    [FirestoreProperty]
    public string IconUrl { get; set; }
    [FirestoreProperty]
    public string License { get; set; }
    [FirestoreProperty]
    public PackageUrls Urls { get;set; }
    [FirestoreProperty]
    public List<PackageReference> Dependencies { get; set; }
    
    [FirestoreProperty]
    public DateTimeOffset Published { get; set; }
}


