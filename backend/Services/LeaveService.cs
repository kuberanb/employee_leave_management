using System.Data;
using EmployeeLeaveManagement.Api.Data;
using EmployeeLeaveManagement.Api.DTOs;
using EmployeeLeaveManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace EmployeeLeaveManagement.Api.Services;

public class RequestException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public class LeaveService(AppDbContext db, TimeProvider clock)
{
    private IQueryable<LeaveRequest> Details => db.LeaveRequests.Include(x => x.Employee).Include(x => x.DecidedBy);
    public async Task<List<LeaveResponse>> Mine(string userId) =>
        (await Details.AsNoTracking().Where(x => x.EmployeeId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(LeaveResponse.From).ToList();
    public async Task<List<LeaveResponse>> Team(string managerId, bool pendingOnly) =>
        (await Details.AsNoTracking().Where(x => x.Employee.ManagerId == managerId && (!pendingOnly || x.Status == LeaveStatus.Pending))
        .OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(LeaveResponse.From).ToList();
    public async Task<LeaveResponse> Get(int id, string userId, bool isManager)
    {
        var leave = await Details.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id &&
            (x.EmployeeId == userId || (isManager && x.Employee.ManagerId == userId)))
            ?? throw new RequestException(404, "Leave request not found.");
        return LeaveResponse.From(leave);
    }
    public static void ValidateDates(ApplyLeaveRequest request, DateOnly today)
    {
        if (request.StartDate < today) throw new RequestException(400, "Start date cannot be in the past.");
        if (request.EndDate < request.StartDate) throw new RequestException(400, "End date must be on or after the start date.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            throw new RequestException(400, "Enter a reason between 1 and 1,000 characters.");
    }
    public async Task<LeaveResponse> Apply(string userId, ApplyLeaveRequest request)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).DateTime);
        ValidateDates(request, today);
        var employee = await db.Users.SingleAsync(x => x.Id == userId);
        if (employee.ManagerId is null) throw new RequestException(400, "Ask your administrator to assign a manager before applying.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.LeaveRequests.AnyAsync(x => x.EmployeeId == userId && x.Status != LeaveStatus.Rejected &&
            x.StartDate <= request.EndDate && x.EndDate >= request.StartDate))
            throw new RequestException(409, "These dates overlap an existing pending or approved request.");
        var leave = new LeaveRequest { EmployeeId = userId, Employee = employee, StartDate = request.StartDate,
            EndDate = request.EndDate, Reason = request.Reason.Trim(), CreatedAt = clock.GetUtcNow().UtcDateTime };
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return LeaveResponse.From(leave);
    }
    public async Task Cancel(int id, string userId)
    {
        var leave = await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == userId)
            ?? throw new RequestException(404, "Leave request not found.");
        if (leave.Status != LeaveStatus.Pending) throw new RequestException(409, "Only pending requests can be cancelled.");
        db.LeaveRequests.Remove(leave);
        await db.SaveChangesAsync();
    }
    public async Task<LeaveResponse> Decide(int id, string managerId, DecisionRequest request)
    {
        if (request.Status is not ("Approved" or "Rejected")) throw new RequestException(400, "Choose Approved or Rejected.");
        var leave = await Details.FirstOrDefaultAsync(x => x.Id == id && x.Employee.ManagerId == managerId)
            ?? throw new RequestException(404, "Leave request not found.");
        if (leave.Status != LeaveStatus.Pending) throw new RequestException(409, "This request has already been decided.");
        byte[] version;
        try { version = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { throw new RequestException(400, "Invalid request version. Refresh the requests."); }
        if (!version.SequenceEqual(leave.RowVersion)) throw new RequestException(409, "This request changed. Refresh and try again.");
        leave.Status = Enum.Parse<LeaveStatus>(request.Status);
        leave.ManagerComment = string.IsNullOrWhiteSpace(request.ManagerComment) ? null : request.ManagerComment.Trim();
        leave.DecidedAt = clock.GetUtcNow().UtcDateTime;
        leave.DecidedById = managerId;
        await db.SaveChangesAsync();
        leave.DecidedBy = await db.Users.FindAsync(managerId);
        return LeaveResponse.From(leave);
    }
}
