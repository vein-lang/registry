namespace core;

using Google.Cloud.Firestore;
using Newtonsoft.Json;

[FirestoreData]
public class RegistryUser : UserBase
{
    [FirestoreDocumentId, JsonProperty("uid")]
    public string UID { get; set; }
}
