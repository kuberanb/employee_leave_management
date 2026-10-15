using System.Security.Claims;
using EmployeeLeaveManagement.Api.DTOs;
using EmployeeLeaveManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EmployeeLeaveManagement.Api.Controllers;

[ApiController, Route("api/manager/leaves"), Authorize(Roles = "Manager")]
public class ManagerController(LeaveService leaves) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<ActionResult<List<LeaveResponse>>> Team() => Ok(await leaves.Team(UserId, false));
    [HttpGet("pending")]
    public async Task<ActionResult<List<LeaveResponse>>> Pending() => Ok(await leaves.Team(UserId, true));
    [HttpPatch("{id:int}/decision")]
    public async Task<ActionResult<LeaveResponse>> Decide(int id, DecisionRequest request) => Ok(await leaves.Decide(id, UserId, request));
}
