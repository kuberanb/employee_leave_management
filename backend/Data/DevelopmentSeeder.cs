using EmployeeLeaveManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace EmployeeLeaveManagement.Api.Data;

public static class DevelopmentSeeder
{
    public static async Task Seed(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Employee", "Manager" })
            if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole(role)));
        async Task<ApplicationUser> Add(string name, string email, string role, string? managerId = null)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { Name = name, UserName = email, Email = email, EmailConfirmed = true, ManagerId = managerId, LockoutEnabled = true };
                Ensure(await users.CreateAsync(user, "Leave@12345"));
                Ensure(await users.AddToRoleAsync(user, role));
            }
            return user;
        }
        var manager = await Add("Priya Sharma", "priya@leave.local", "Manager");
        var employee = await Add("Arun Kumar", "arun@leave.local", "Employee", manager.Id);
        await Add("Meena Rao", "meena@leave.local", "Employee", manager.Id);
        if (!await db.LeaveRequests.AnyAsync(x => x.EmployeeId == employee.Id))
        {
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Kolkata"));
            db.LeaveRequests.AddRange(
                new LeaveRequest { EmployeeId = employee.Id, StartDate = today.AddDays(-14), EndDate = today.AddDays(-12), Reason = "Family celebration", Status = LeaveStatus.Approved, ManagerComment = "Enjoy your time with family.", DecidedById = manager.Id, DecidedAt = DateTime.UtcNow.AddDays(-16), CreatedAt = DateTime.UtcNow.AddDays(-18) },
                new LeaveRequest { EmployeeId = employee.Id, StartDate = today.AddDays(7), EndDate = today.AddDays(8), Reason = "Personal appointment", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new LeaveRequest { EmployeeId = employee.Id, StartDate = today.AddDays(-7), EndDate = today.AddDays(-7), Reason = "Personal work", Status = LeaveStatus.Rejected, ManagerComment = "Please choose a day outside the team release.", DecidedById = manager.Id, DecidedAt = DateTime.UtcNow.AddDays(-9), CreatedAt = DateTime.UtcNow.AddDays(-10) });
            await db.SaveChangesAsync();
        }
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
