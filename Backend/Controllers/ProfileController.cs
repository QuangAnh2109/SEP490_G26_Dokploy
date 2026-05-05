using Backend.Common;
using Backend.DTOs.Profile;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/profile")]
[ApiController]
public class ProfileController(IProfileService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetProfile()
        => (await service.GetProfileAsync(currentUser.UserId)).ToActionResult(this);

    [HttpPut]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDTO dto)
        => (await service.UpdateProfileAsync(currentUser.UserId, dto)).ToActionResult(this);

    [HttpPut("change-password")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        => (await service.ChangePasswordAsync(currentUser.UserId, dto)).ToActionResult(this);
}
