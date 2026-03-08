using System.Text;
using AMWatch.Application.Interfaces;
using AMWatch.Application.Services;
using AMWatch.Domain.Interfaces;
using AMWatch.Infrastructure.Repositories;
using AMWatch.Infrastructure.Services;
using AMWatch.Persistence;
using AMWatch.Presentation.Auth;
using AMWatch.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=amwatch.db"));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TimeTrackingService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ReportingService>();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IJwtTokenGenerator>(sp => sp.GetRequiredService<JwtTokenService>());
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

var secret = builder.Configuration["Jwt:Secret"] ?? "dev-only-secret-key-change-in-production";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
