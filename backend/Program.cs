using System.Security.Cryptography;
using System.Text;
using EmployeeLeaveManagement.Api.Data;
using EmployeeLeaveManagement.Api.Models;
using EmployeeLeaveManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Configure Jwt:Key before starting the API.");
    builder.Configuration["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
if (Encoding.UTF8.GetByteCount(builder.Configuration["Jwt:Key"]!) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<LeaveService>();
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (RequestException ex) { await Error(ex.StatusCode, ex.Message); }
    catch (DbUpdateConcurrencyException) { await Error(409, "This request changed. Refresh and try again."); }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Request failed");
        await Error(500, "Something went wrong. Please try again.");
    }
    async Task Error(int status, string message)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message });
    }
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    await DevelopmentSeeder.Seed(scope.ServiceProvider);
}
else app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }));
app.Run();
public partial class Program;
