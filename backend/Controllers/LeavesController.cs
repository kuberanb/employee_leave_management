using System.Security.Claims;
using EmployeeLeaveManagement.Api.DTOs;
using EmployeeLeaveManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EmployeeLeaveManagement.Api.Controllers;

[ApiController, Route("api/leaves"), Authorize]
public class LeavesController(LeaveService leaves) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("my")]
    public async Task<ActionResult<List<LeaveResponse>>> Mine() => Ok(await leaves.Mine(UserId));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveResponse>> Get(int id) => Ok(await leaves.Get(id, UserId, User.IsInRole("Manager")));
    [HttpPost, Authorize(Roles = "Employee")]
    public async Task<ActionResult<LeaveResponse>> Apply(ApplyLeaveRequest request)
    {
        var leave = await leaves.Apply(UserId, request);
        return CreatedAtAction(nameof(Get), new { id = leave.Id }, leave);
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> Cancel(int id) { await leaves.Cancel(id, UserId); return NoContent(); }
}
