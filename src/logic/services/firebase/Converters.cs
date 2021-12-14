namespace core.services;

using Google.Cloud.Firestore;
using Newtonsoft.Json;
using vein.project;

public class GuidConverter : IFirestoreConverter<Guid>
{
    public object ToFirestore(Guid value) => $"{value}";

    public Guid FromFirestore(object value) => Guid.Parse($"{value}");
}

public class AutoConverter<T> : IFirestoreConverter<T>
{
    public object ToFirestore(T value)
    {
        var json = JsonConvert.SerializeObject(value);
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }

    public T FromFirestore(object value)
    {
        var result = JsonConvert.SerializeObject(value);
        return JsonConvert.DeserializeObject<T>(result);
    }
}


public class PackageReferenceConverter : AutoConverter<PackageReference>
{
}

public class PackageAuthorConverter : AutoConverter<PackageAuthor>
{
}

public class PackageUrlsConverter : AutoConverter<PackageUrls>
{
}
