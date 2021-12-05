namespace core;

using Google.Cloud.Firestore;
using Newtonsoft.Json;

public class UserBase
{
    [FirestoreProperty, JsonProperty("sub")]
    public string Sub { get; set; }

    [FirestoreProperty, JsonProperty("nickname")]
    public string Login { get; set; }

    [FirestoreProperty, JsonProperty("name")]
    public string Name { get; set; }

    [FirestoreProperty, JsonProperty("picture")]
    public string Avatar { get; set; }

    [FirestoreProperty, JsonProperty("email")]
    public string Email { get; set; }
}
