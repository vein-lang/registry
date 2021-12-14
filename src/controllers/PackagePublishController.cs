namespace core.controllers;

using core.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Route("@/publish"), Authorize]
[ApiController]
public class PackagePublishController : Controller
{
    private readonly IPackageIndexingService _indexer;
    private readonly ILogger<PackagePublishController> _logger;
    private readonly IUserService _userService;

    public PackagePublishController(
        IPackageIndexingService indexer,
        ILogger<PackagePublishController> logger,
        IUserService userService)
    {
        _indexer = indexer;
        _logger = logger;
        _userService = userService;
    }

    public async Task Upload(CancellationToken cancellationToken)
    {
        try
        {
            using (var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken))
            {
                if (uploadStream == null)
                {
                    HttpContext.Response.StatusCode = 400;
                    return;
                }

                var me = await _userService.GetMeAsync();
                var result = await _indexer.IndexAsync(uploadStream, me, cancellationToken);

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

                    case PackageIndexingResult.AccessDenied:
                        HttpContext.Response.StatusCode = 403;
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
