using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/admin/users")]
[ApiController]
[Authorize(Roles = RoleIds.Admin)]
public class AdminUserController(IAdminUserService adminUserService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminUserListQuery query) =>
        (await adminUserService.ListAsync(query)).ToActionResult(this);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) =>
        (await adminUserService.GetAsync(id)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request) =>
        (await adminUserService.CreateAsync(request)).ToActionResult(this);

    [HttpPatch("{id:int}/lock")]
    public async Task<IActionResult> Lock(int id) =>
        (await adminUserService.LockAsync(id, currentUser.UserId)).ToActionResult(this);

    [HttpPatch("{id:int}/unlock")]
    public async Task<IActionResult> Unlock(int id) =>
        (await adminUserService.UnlockAsync(id)).ToActionResult(this);

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id) =>
        (await adminUserService.ResetPasswordAsync(id)).ToActionResult(this);
}
