using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeLeaveManagement.Api.DTOs;
using EmployeeLeaveManagement.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
namespace EmployeeLeaveManagement.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users, IConfiguration config) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || await users.IsLockedOutAsync(user)) return Unauthorized(new { message = "Invalid email or password." });
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid email or password." });
        }
        await users.ResetAccessFailedCountAsync(user);
        var roles = await users.GetRolesAsync(user);
        var role = roles.Contains("Manager") ? "Manager" : "Employee";
        var expires = DateTime.UtcNow.AddHours(2);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.Name), new(ClaimTypes.Role, role) };
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, new UserResponse(user.Id, user.Name, user.Email!, role)));
    }
    [HttpGet("me"), Authorize]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await users.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (user is null) return Unauthorized();
        return Ok(new UserResponse(user.Id, user.Name, user.Email!, User.IsInRole("Manager") ? "Manager" : "Employee"));
    }
}
