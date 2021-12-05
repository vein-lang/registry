namespace core.services;

using Google.Cloud.Firestore;

public class FirestoreGuidConverter : IFirestoreConverter<Guid>
{
    public object ToFirestore(Guid value) => $"{value}";

    public Guid FromFirestore(object value) => Guid.Parse($"{value}");
}
