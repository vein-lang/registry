namespace core.controllers;

using core.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Route("@/publish")]
public class PackagePublishController : Controller
{
    private readonly IAuthenticationService _authentication;
    private readonly IPackageService _packages;
    private readonly IPackageIndexingService _indexer;
    private readonly IOptionsSnapshot<RegistryOptions> _options;
    private readonly ILogger<PackagePublishController> _logger;

    public PackagePublishController(
        IAuthenticationService authentication,
        IPackageIndexingService indexer,
        IPackageService packages,
        IOptionsSnapshot<RegistryOptions> options,
        ILogger<PackagePublishController> logger)
    {
        _authentication = authentication;
        _indexer = indexer;
        _packages = packages;
        _options = options;
        _logger = logger;
    }

    public async Task Upload(CancellationToken cancellationToken)
    {
        if (_options.Value.IsReadOnlyMode ||
            !await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
        {
            HttpContext.Response.StatusCode = 401;
            return;
        }

        try
        {
            using (var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken))
            {
                if (uploadStream == null)
                {
                    HttpContext.Response.StatusCode = 400;
                    return;
                }

                var result = await _indexer.IndexAsync(uploadStream, cancellationToken);

                switch (result)
                {
                    case PackageIndexingResult.InvalidPackage:
                        HttpContext.Response.StatusCode = 400;
                        break;

                    case PackageIndexingResult.PackageAlreadyExists:
                        HttpContext.Response.StatusCode = 409;
                        break;

                    case PackageIndexingResult.Success:
                        HttpContext.Response.StatusCode = 201;
                        break;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown during package upload");

            HttpContext.Response.StatusCode = 500;
        }
    }
}
