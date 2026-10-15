using Microsoft.AspNetCore.Identity;
namespace EmployeeLeaveManagement.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
}
