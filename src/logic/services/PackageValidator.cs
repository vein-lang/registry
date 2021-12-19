namespace core.services;

using ProfanityFilter;
using vein.project.shards;

public class PackageValidator
{
    private const long MaxAllowedIconLengthForUploading = 1024 * 1024; // 1 MB
    private const long MaxAllowedContentForUploading = 1024 * 1024 * 300; // 300 MB
    private static ProfanityFilter _censor = new ();

    private static readonly List<string> Reserved = new()
    {
        "com0", "com1", "com2",
        "com3", "com4", "com5",
        "com6","com7", "com8", "com9",
        "builtins", "collections",
        "debug", "std", "test", "bump",
        "nul", "null", "prn", "aux",
        "vin", "http", "grpc", "logger",
        "core", "kernel", "live", "docker"
    };

    public static async Task ValidateExistAsync(Shard shard)
    {
        if (shard.Description?.Length > 70)
            throw new PackageValidatorException($"{nameof(Shard.Description)} is more 70 symbols.");
        if (shard.Name?.Length > 30)
            throw new PackageValidatorException($"{nameof(Shard.Name)} is more 30 symbols.");
        if (shard.Size > MaxAllowedContentForUploading)
            throw new PackageValidatorException($"Size of package is more 300 MB.");

        using var icon = await shard.GetIconAsync();

        if (icon.Length > MaxAllowedIconLengthForUploading)
            throw new PackageValidatorException($"Size of icon is more 1 MB.");

        if (_censor.ContainsProfanity(shard.Description))
            throw new PackageValidatorException($"{nameof(Shard.Description)} is contained banned words.");
        
    }


    public static void ValidateNewPackage(Package package)
    {
        if (IsReservedName(package))
            throw new PackageValidatorException(
                $"Name '{package.Name}' has been reserved.\n " +
                $"If you want to use a reserved package name, please create an issue in " +
                $"https://github.com/vein-lang/registry");
        if (_censor.ContainsProfanity(package.Name))
            throw new PackageValidatorException($"{nameof(Shard.Name)} is contained banned words.");
    }


    public static bool IsReservedName(Package package)
        => Reserved.Any(x => package.Name.Equals(x, StringComparison.InvariantCultureIgnoreCase)) ||
           package.Name.Length < 2 ||
           package.Name.StartsWith("vein", StringComparison.InvariantCultureIgnoreCase);
}


public class PackageValidatorException : Exception
{
    public PackageValidatorException(string msg) : base(msg) { }
}
