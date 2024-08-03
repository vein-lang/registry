namespace core.controllers;

using services;
using Microsoft.AspNetCore.Mvc;
using Sentry;

[Route("@/publish"), ApiKeyAuth]
[ApiController]
public class PackagePublishController(
    IPackageIndexingService indexer,
    ILogger<PackagePublishController> logger,
    IUserService userService,
    IHub sentryHub)
    : Controller
{
    public async Task Upload(CancellationToken cancellationToken)
    {
        var task = sentryHub.GetSpan()?.StartChild($"{nameof(PackagePublishController)}::{nameof(Upload)}");
        try
        {
            await using var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken);
            if (uploadStream == null)
            {
                HttpContext.Response.StatusCode = 400;
                task?.Finish(SpanStatus.InvalidArgument);
                return;
            }

            var me = await userService.GetMeAsync(cancellationToken);
            var result = await indexer.IndexAsync(uploadStream, me, cancellationToken);

            switch (result)
            {
                case PackageIndexingResult.InternalError:
                    HttpContext.Response.StatusCode = 500;
                    await HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = $"Internal Error.",
                        traceId = HttpContext.Request.Headers["traceparent"].ToString()
                    }, cancellationToken: cancellationToken);
                    task?.Finish(SpanStatus.InternalError);
                    break;
                case PackageIndexingResult.InvalidPackage:
                    HttpContext.Response.StatusCode = 400;
                    await HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = $"Invalid package.",
                        traceId = HttpContext.Request.Headers["traceparent"].ToString()
                    }, cancellationToken: cancellationToken);
                    task?.Finish(SpanStatus.InvalidArgument);
                    break;

                case PackageIndexingResult.PackageAlreadyExists:
                    HttpContext.Response.StatusCode = 409;
                    await HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = $"Package already exist!",
                        traceId = HttpContext.Request.Headers["traceparent"].ToString()
                    }, cancellationToken: cancellationToken);
                    task?.Finish(SpanStatus.AlreadyExists);
                    break;

                case PackageIndexingResult.Success:
                    HttpContext.Response.StatusCode = 201;
                    await HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = $"Succes!",
                        traceId = HttpContext.Request.Headers["traceparent"].ToString()
                    }, cancellationToken: cancellationToken);
                    task?.Finish(SpanStatus.Ok);
                    break;

                case PackageIndexingResult.AccessDenied:
                    HttpContext.Response.StatusCode = 403;
                    await HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = $"Access Denied",
                        traceId = HttpContext.Request.Headers["traceparent"].ToString()
                    }, cancellationToken: cancellationToken);
                    task?.Finish(SpanStatus.PermissionDenied);
                    break;
            }
        }
        catch (PackageValidatorException e)
        {
            logger.LogError(e, "Exception thrown during package validation");
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                message = e.Message,
                traceId = HttpContext.Request.Headers["traceparent"].ToString()
            }, cancellationToken: cancellationToken);
            task?.Finish(e, SpanStatus.InternalError);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception thrown during package upload");
            
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                message = $"Exception thrown during package upload.",
                traceId = HttpContext.Request.Headers["traceparent"].ToString()
            }, cancellationToken: cancellationToken);
            HttpContext.Response.StatusCode = 500;
            task?.Finish(e, SpanStatus.InternalError);
        }
    }
}
