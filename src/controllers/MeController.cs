namespace core.controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using services;

[Authorize]
[ApiController]
public class MeController(IUserService _userService, IPackageService packageService) : Controller
{
    [HttpGet("@/me")]
    public async Task<IActionResult> GetMe(CancellationToken token)
        => Json(await _userService.GetMeAsync(token));

    [HttpGet("@/me/packages")]
    public async Task<IActionResult> GetMePackages(CancellationToken token)
    {
        var me = await _userService.GetMeAsync(token);
        var packages = await packageService.GetLatestPackagesByUserAsync(me, token);
        return Json(packages);
    }

    [HttpPost("@/me/token/new")]
    public async Task<IActionResult> CreateApiKeyAsync([FromQuery]string name)
        => Json(await _userService.GenerateApiKeyAsync(name, TimeSpan.FromDays(60)));

    [HttpGet("@/me/token")]
    public async Task<IActionResult> GetApiKeys()
        => Json(await _userService.GetApiKeysAsync());

    [HttpDelete("@/me/token/{uid}")]
    public async Task<IActionResult> DeleteApiKey(string uid)
    {
        try
        {
            await _userService.DeleteApiKeyAsync(uid);
            return Json(true);
        }
        catch
        {
            return Json(false);
        }
    }
}
