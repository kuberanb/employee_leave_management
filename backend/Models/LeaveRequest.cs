namespace EmployeeLeaveManagement.Api.Models;

public enum LeaveStatus { Pending, Approved, Rejected }
public class LeaveRequest
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public ApplicationUser Employee { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? ManagerComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public string? DecidedById { get; set; }
    public ApplicationUser? DecidedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
