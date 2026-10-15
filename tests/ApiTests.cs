using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmployeeLeaveManagement.Api.Data;
using EmployeeLeaveManagement.Api.DTOs;
using EmployeeLeaveManagement.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EmployeeLeaveManagement.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private bool cleaned;
    private readonly string database = "EmployeeLeaveManagementTests_" + Guid.NewGuid().ToString("N");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Server=(localdb)\\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True",
            ["Jwt:Key"] = "test-signing-key-for-leave-api-at-least-32-bytes"
        }));
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
                $"Server=(localdb)\\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True"));
        });
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !cleaned)
        {
            cleaned = true;
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.GetDbConnection().Database == database) db.Database.EnsureDeleted();
        }
        base.Dispose(disposing);
    }
}

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpClient> Login(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Leave@12345"));
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }
    [Fact]
    public async Task AnonymousRequestsAreDenied()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/leaves/my")).StatusCode);
    }
    [Fact]
    public async Task WrongPasswordIsDenied()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("arun@leave.local", "incorrect"))).StatusCode);
    }
    [Fact]
    public async Task EmployeeCannotUseManagerEndpoints()
    {
        using var client = await Login("arun@leave.local");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/manager/leaves/pending")).StatusCode);
    }
    [Fact]
    public async Task EmployeeCannotReadOrCancelAnotherEmployeesRequest()
    {
        using var arun = await Login("arun@leave.local");
        var leave = (await arun.GetFromJsonAsync<List<LeaveResponse>>("/api/leaves/my"))!.First();
        using var meena = await Login("meena@leave.local");
        Assert.Equal(HttpStatusCode.NotFound, (await meena.GetAsync($"/api/leaves/{leave.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await meena.DeleteAsync($"/api/leaves/{leave.Id}")).StatusCode);
    }
    [Fact]
    public async Task ApplyApproveAndPreventFurtherChanges()
    {
        using var employee = await Login("meena@leave.local");
        var date = new DateOnly(2090, 1, 12);
        var response = await employee.PostAsJsonAsync("/api/leaves", new ApplyLeaveRequest(date, date.AddDays(1), "Family trip"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var leave = (await response.Content.ReadFromJsonAsync<LeaveResponse>())!;
        Assert.Equal("Pending", leave.Status);
        Assert.Equal("meena@leave.local", leave.EmployeeEmail);
        Assert.Equal(DateTimeKind.Utc, leave.CreatedAt.Kind);
        Assert.Equal(2, leave.Days);
        Assert.Equal(HttpStatusCode.Conflict, (await employee.PostAsJsonAsync("/api/leaves", new ApplyLeaveRequest(date, date, "Overlap"))).StatusCode);
        using var manager = await Login("priya@leave.local");
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PatchAsJsonAsync($"/api/manager/leaves/{leave.Id}/decision", new DecisionRequest("Approved", null, "AAAA"))).StatusCode);
        var approval = await manager.PatchAsJsonAsync($"/api/manager/leaves/{leave.Id}/decision", new DecisionRequest("Approved", "Enjoy your break", leave.RowVersion));
        approval.EnsureSuccessStatusCode();
        var approved = (await approval.Content.ReadFromJsonAsync<LeaveResponse>())!;
        Assert.Equal("Approved", approved.Status);
        Assert.Equal("Priya Sharma", approved.DecidedByName);
        Assert.Equal(DateTimeKind.Utc, approved.DecidedAt!.Value.Kind);
        Assert.Equal(HttpStatusCode.Conflict, (await employee.DeleteAsync($"/api/leaves/{leave.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PatchAsJsonAsync($"/api/manager/leaves/{leave.Id}/decision", new DecisionRequest("Rejected", null, approved.RowVersion))).StatusCode);
    }
    [Fact]
    public async Task PendingRequestsCanBeCancelled()
    {
        using var employee = await Login("meena@leave.local");
        var date = new DateOnly(2091, 2, 10);
        var created = await employee.PostAsJsonAsync("/api/leaves", new ApplyLeaveRequest(date, date, "Appointment"));
        var leave = (await created.Content.ReadFromJsonAsync<LeaveResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await employee.DeleteAsync($"/api/leaves/{leave.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await employee.GetAsync($"/api/leaves/{leave.Id}")).StatusCode);
    }
    [Fact]
    public async Task RejectedDatesCanBeRequestedAgain()
    {
        using var employee = await Login("meena@leave.local");
        var date = new DateOnly(2092, 3, 10);
        var created = await employee.PostAsJsonAsync("/api/leaves", new ApplyLeaveRequest(date, date, "Personal work"));
        var leave = (await created.Content.ReadFromJsonAsync<LeaveResponse>())!;
        using var manager = await Login("priya@leave.local");
        var rejected = await manager.PatchAsJsonAsync($"/api/manager/leaves/{leave.Id}/decision", new DecisionRequest("Rejected", "Please reschedule", leave.RowVersion));
        Assert.Equal("Rejected", (await rejected.Content.ReadFromJsonAsync<LeaveResponse>())!.Status);
        Assert.Equal(HttpStatusCode.Created, (await employee.PostAsJsonAsync("/api/leaves", new ApplyLeaveRequest(date, date, "Updated reason"))).StatusCode);
    }
    [Fact]
    public async Task ManagerCannotApproveOutsideTheirTeam()
    {
        using var employee = await Login("arun@leave.local");
        var leave = (await employee.GetFromJsonAsync<List<LeaveResponse>>("/api/leaves/my"))!.First(x => x.Status == "Pending");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var manager = new ApplicationUser { Name = "Other Manager", UserName = "other@leave.local", Email = "other@leave.local" };
            Assert.True((await users.CreateAsync(manager, "Leave@12345")).Succeeded);
            Assert.True((await users.AddToRoleAsync(manager, "Manager")).Succeeded);
        }
        using var other = await Login("other@leave.local");
        Assert.Empty((await other.GetFromJsonAsync<List<LeaveResponse>>("/api/manager/leaves/pending"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PatchAsJsonAsync($"/api/manager/leaves/{leave.Id}/decision", new DecisionRequest("Approved", null, leave.RowVersion))).StatusCode);
    }
}
