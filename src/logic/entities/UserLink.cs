namespace core;

using Google.Cloud.Firestore;

[FirestoreData]
public class UserLink
{
    [FirestoreDocumentId]
    public string Sub { get; set; }
    [FirestoreProperty]
    public string InternalUID { get; set; }
    [FirestoreProperty]
    public bool RequestReIndexing { get; set; }
}
