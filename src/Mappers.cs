namespace core;

using AutoMapper;
using NuGet.Versioning;
using services.searchs;
using vein.project;

public class Mappers : Profile
{
    public Mappers()
    {
        CreateMap<Package, PackageEntity>()
            .ForMember(x => x.Id, x => x.MapFrom(z => z.Name))
            .ForMember(x => x.OriginalVersion, x => x.MapFrom(z => z.Version.OriginalVersion))
            .ForMember(x => x.NormalizedVersion, x => x.MapFrom(z => z.Version.ToNormalizedString()));

        CreateMap<PackageEntity, Package>()
            .ForMember(x => x.Name, x => x.MapFrom(z => z.Id))
            .ForMember(x => x.Version, x => x.MapFrom(z => NuGetVersion.Parse(z.OriginalVersion)));

        CreateMap<PackageManifest, Package>();
        CreateMap<Package, PackageManifest>();
    }
}
