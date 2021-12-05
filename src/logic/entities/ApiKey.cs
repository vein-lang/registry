namespace core;

using Google.Cloud.Firestore;
using Newtonsoft.Json;

public class ApiKey
{
    [FirestoreDocumentId, JsonProperty("uid")]
    public string UID { get; set; }

    [FirestoreProperty("owner"), JsonProperty]
    public string UserOwner { get; set; }

    [FirestoreDocumentCreateTimestamp, JsonProperty]
    public DateTimeOffset CreationDate { get; set; }
    [FirestoreProperty("eol"), JsonProperty]
    public TimeSpan EndOfLife { get; set; }

    [FirestoreProperty, JsonProperty]
    public string Name { get; set; }
}
