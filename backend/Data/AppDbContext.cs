using EmployeeLeaveManagement.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace EmployeeLeaveManagement.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(x => x.Name).HasMaxLength(100).IsRequired();
            user.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
            user.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveRequest>(leave =>
        {
            leave.ToTable("LeaveRequests", table =>
            {
                table.HasCheckConstraint("CK_LeaveRequests_Dates", "[EndDate] >= [StartDate]");
                table.HasCheckConstraint("CK_LeaveRequests_Status", "[Status] IN ('Pending', 'Approved', 'Rejected')");
                table.HasCheckConstraint("CK_LeaveRequests_Reason", "LEN(LTRIM(RTRIM([Reason]))) > 0");
            });
            leave.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            leave.Property(x => x.ManagerComment).HasMaxLength(1000);
            leave.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            leave.Property(x => x.RowVersion).IsRowVersion();
            leave.HasOne(x => x.Employee).WithMany(x => x.LeaveRequests).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            leave.HasOne(x => x.DecidedBy).WithMany().HasForeignKey(x => x.DecidedById).OnDelete(DeleteBehavior.Restrict);
            leave.HasIndex(x => new { x.EmployeeId, x.CreatedAt });
            leave.HasIndex(x => new { x.Status, x.EmployeeId });
        });
    }
}
