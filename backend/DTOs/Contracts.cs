using System.ComponentModel.DataAnnotations;
using EmployeeLeaveManagement.Api.Models;
namespace EmployeeLeaveManagement.Api.DTOs;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record UserResponse(string Id, string Name, string Email, string Role);
public record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);
public record ApplyLeaveRequest(DateOnly StartDate, DateOnly EndDate, [Required, StringLength(1000)] string Reason);
public record DecisionRequest([Required] string Status, [StringLength(1000)] string? ManagerComment, [Required] string RowVersion);
public record LeaveResponse(int Id, string EmployeeName, string EmployeeEmail, DateOnly StartDate,
    DateOnly EndDate, int Days, string Reason, string Status, string? ManagerComment,
    DateTime CreatedAt, DateTime? DecidedAt, string? DecidedByName, string RowVersion)
{
    public static LeaveResponse From(LeaveRequest leave) => new(leave.Id, leave.Employee.Name,
        leave.Employee.Email!, leave.StartDate, leave.EndDate,
        leave.EndDate.DayNumber - leave.StartDate.DayNumber + 1, leave.Reason, leave.Status.ToString(),
        leave.ManagerComment, DateTime.SpecifyKind(leave.CreatedAt, DateTimeKind.Utc),
        leave.DecidedAt is { } decidedAt ? DateTime.SpecifyKind(decidedAt, DateTimeKind.Utc) : null, leave.DecidedBy?.Name,
        Convert.ToBase64String(leave.RowVersion));
}
