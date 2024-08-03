namespace core;

using Google.Cloud.Firestore;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509.Qualified;

[FirestoreData]
public class ApiKey
{
    [FirestoreDocumentId, JsonProperty("uid")]
    public string UID { get; set; }

    [FirestoreProperty("owner"), JsonProperty]
    public string UserOwner { get; set; }

    [FirestoreDocumentCreateTimestamp, JsonProperty("creationDate")]
    public DateTimeOffset CreationDate { get; set; }

    [FirestoreProperty("eol", ConverterType = typeof(TimeSpanConverter)), JsonProperty]
    public TimeSpan EndOfLife { get; set; }

    [JsonProperty("expiresDate")]
    public DateTimeOffset ExpiresDate => CreationDate + EndOfLife;

    [FirestoreProperty, JsonProperty("name")]
    public string Name { get; set; }
}


[FirestoreData]
public class UserDetails
{
    [FirestoreDocumentId, JsonProperty("uid")]
    public string UID { get; set; }

    [FirestoreProperty("owner"), JsonProperty]
    public string UserOwner { get; set; }

    [FirestoreDocumentCreateTimestamp, JsonProperty("creationDate")]
    public DateTimeOffset CreationDate { get; set; }

    [FirestoreProperty("isAllowedPublishWorkloads"), JsonProperty("isAllowedPublishWorkloads")]
    public bool IsAllowedPublishWorkloads { get; set; }

    [FirestoreProperty("isAllowedPublishServicePackage"), JsonProperty("isAllowedPublishServicePackage")]
    public bool IsAllowedPublishServicePackage { get; set; }

    [FirestoreProperty("isAllowedSkipPublishVerification"), JsonProperty("isAllowedSkipPublishVerification")]
    public bool IsAllowedSkipPublishVerification { get; set; }
}


public class TimeSpanConverter : IFirestoreConverter<TimeSpan>
{
    public TimeSpan FromFirestore(object value) => TimeSpan.Parse((string)value);
    public object ToFirestore(TimeSpan value) => value.ToString("c");
}
