namespace core.controllers;

using System.Net.Http.Headers;
using Flurl.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using services;

[Authorize]
[ApiController]
public class MeController : Controller
{
    private readonly IUserService _userService;

    public MeController(IUserService _userService) => this._userService = _userService;

    [HttpGet("@/me")]
    public async Task<IActionResult> GetMe()
        => Json(await _userService.GetMeAsync());
}
