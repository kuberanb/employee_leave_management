using EmployeeLeaveManagement.Api.DTOs;
using EmployeeLeaveManagement.Api.Services;
namespace EmployeeLeaveManagement.Tests;

public class LeaveRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 13);
    [Theory]
    [InlineData(-1, 2, "Personal appointment")]
    [InlineData(2, 1, "Personal appointment")]
    [InlineData(1, 2, "   ")]
    public void InvalidApplicationsAreRejected(int start, int end, string reason)
    {
        var request = new ApplyLeaveRequest(Today.AddDays(start), Today.AddDays(end), reason);
        Assert.Equal(400, Assert.Throws<RequestException>(() => LeaveService.ValidateDates(request, Today)).StatusCode);
    }
    [Fact]
    public void SameDayLeaveIsAllowed() => LeaveService.ValidateDates(new(Today, Today, "Appointment"), Today);
    [Fact]
    public void LongReasonIsRejected() => Assert.Throws<RequestException>(() =>
        LeaveService.ValidateDates(new(Today, Today, new string('a', 1001)), Today));
}
