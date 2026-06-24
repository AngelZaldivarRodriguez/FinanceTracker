using System.Text;
using System.Text.Json.Serialization;
using FinanceTracker.API.Common.Extensions;
using FinanceTracker.API.Infrastructure.Auth;
using FinanceTracker.API.Infrastructure.BackgroundJobs;
using FinanceTracker.API.Infrastructure.Email;
using FinanceTracker.API.Infrastructure.Persistence;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"]!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtService>();

// Email
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<EmailService>();

// Jobs
builder.Services.AddScoped<BudgetAlertJob>();
builder.Services.AddScoped<CreditCardPaymentReminderJob>();
builder.Services.AddScoped<LoanPaymentReminderJob>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<BudgetAlertJob>(
    "budget-alerts",
    job => job.CheckBudgetAlerts(),
    Cron.Hourly);

RecurringJob.AddOrUpdate<CreditCardPaymentReminderJob>(
    "credit-card-reminders",
    job => job.CheckPaymentsDue(),
    Cron.Daily(8)); // todos los días a las 8am UTC (2am México)

RecurringJob.AddOrUpdate<LoanPaymentReminderJob>(
    "loan-reminders",
    job => job.CheckPaymentsDue(),
    Cron.Daily(8));

app.MapFeatureEndpoints();

app.Run();
