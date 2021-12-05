namespace core;

using Google.Cloud.Firestore;

[FirestoreData]
public class PackageLink
{
    [FirestoreProperty]
    public Guid UserID { get; set; }
    [FirestoreProperty]
    public string PackageID { get; set; }
    [FirestoreProperty]
    public AuthoringType AccessType { get; set; }
}