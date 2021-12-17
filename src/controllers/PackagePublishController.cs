namespace core.controllers;

using core.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sentry;

[Route("@/publish"), Authorize]
[ApiController]
public class PackagePublishController : Controller
{
    private readonly IPackageIndexingService _indexer;
    private readonly ILogger<PackagePublishController> _logger;
    private readonly IUserService _userService;
    private readonly IHub _sentryHub;

    public PackagePublishController(
        IPackageIndexingService indexer,
        ILogger<PackagePublishController> logger,
        IUserService userService,
        IHub sentryHub)
    {
        _indexer = indexer;
        _logger = logger;
        _userService = userService;
        _sentryHub = sentryHub;
    }

    public async Task Upload(CancellationToken cancellationToken)
    {
        var task = _sentryHub.GetSpan()?.StartChild($"{nameof(PackagePublishController)}::{nameof(Upload)}");
        try
        {
            using (var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken))
            {
                if (uploadStream == null)
                {
                    HttpContext.Response.StatusCode = 400;
                    task?.Finish(SpanStatus.InvalidArgument);
                    return;
                }

                var me = await _userService.GetMeAsync();
                var result = await _indexer.IndexAsync(uploadStream, me, cancellationToken);

                switch (result)
                {
                    case PackageIndexingResult.InvalidPackage:
                        HttpContext.Response.StatusCode = 400;
                        await HttpContext.Response.WriteAsJsonAsync(new
                        {
                            message = $"Invalid package.",
                            traceId = HttpContext.Request.Headers["traceparent"].ToString()
                        });
                        task?.Finish(SpanStatus.InvalidArgument);
                        break;

                    case PackageIndexingResult.PackageAlreadyExists:
                        HttpContext.Response.StatusCode = 409;
                        await HttpContext.Response.WriteAsJsonAsync(new
                        {
                            message = $"Package already exist!",
                            traceId = HttpContext.Request.Headers["traceparent"].ToString()
                        });
                        task?.Finish(SpanStatus.AlreadyExists);
                        break;

                    case PackageIndexingResult.Success:
                        HttpContext.Response.StatusCode = 201;
                        await HttpContext.Response.WriteAsJsonAsync(new
                        {
                            message = $"Succes!",
                            traceId = HttpContext.Request.Headers["traceparent"].ToString()
                        });
                        task?.Finish(SpanStatus.Ok);
                        break;

                    case PackageIndexingResult.AccessDenied:
                        HttpContext.Response.StatusCode = 403;
                        await HttpContext.Response.WriteAsJsonAsync(new
                        {
                            message = $"Access Denied",
                            traceId = HttpContext.Request.Headers["traceparent"].ToString()
                        });
                        task?.Finish(SpanStatus.PermissionDenied);
                        break;
                }
            }
        }
        catch (PackageValidatorException e)
        {
            _logger.LogError(e, "Exception thrown during package validation");
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                message = e.Message,
                traceId = HttpContext.Request.Headers["traceparent"].ToString()
            });
            task?.Finish(e, SpanStatus.InternalError);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown during package upload");
            
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                message = $"Exception thrown during package upload.",
                traceId = HttpContext.Request.Headers["traceparent"].ToString()
            });
            HttpContext.Response.StatusCode = 500;
            task?.Finish(e, SpanStatus.InternalError);
        }
    }
}
