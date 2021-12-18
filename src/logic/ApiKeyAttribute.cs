namespace core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using services;

[AttributeUsage(validOn: AttributeTargets.Class)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string APIKEYNAME = "X-VEIN-API-KEY";
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey(APIKEYNAME))
        {
            context.Result = new ContentResult()
            {
                StatusCode = 401,
                Content = "Api Key was not provided"
            };
            return;
        }
 
        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();

        var result = await userService.GetMeAsync();
 
        if (result == null)
        {
            context.Result = new ContentResult()
            {
                StatusCode = 401,
                Content = "Api Key is not valid"
            };
            return;
        }
 
        await next();
    }
}
