using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TaskManagement.Api.Middleware;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Diagnostics.HealthChecks;




JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository & UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IProjectService, ProjectService>();  // ← ADD THIS
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
         NameClaimType = "sub"
    };
});

builder.Services.AddAuthorization();
builder.Services.AddResponseCaching();

builder.Services.AddControllers();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description = @"
# Task Management System API

A comprehensive REST API for team collaboration and task management.

## Features
- **Authentication**: JWT-based authentication with role-based access control
- **Teams**: Create and manage teams with Owner/Manager/Member roles
- **Projects**: Organize work into projects within teams
- **Tasks**: Create, assign, and track tasks with status workflows
- **Advanced Filtering**: Filter tasks by status, priority, assignee, and date range
- **Pagination**: Efficient data loading with metadata

## Authentication
All endpoints (except `/api/v1/auth/register` and `/api/v1/auth/login`) require authentication.

1. Register or login to get a JWT token
2. Click 'Authorize' button above
3. Enter: `Bearer YOUR_TOKEN_HERE`
4. Click 'Authorize' then 'Close'

## Common Workflows

### 1. Getting Started
```
POST /api/v1/auth/register → Get token
POST /api/v1/teams → Create team (you become Owner)
POST /api/v1/teams/{teamId}/members → Add members
POST /api/v1/teams/{teamId}/projects → Create project
POST /api/v1/projects/{projectId}/tasks → Create tasks
```

### 2. Managing Tasks
```
GET /api/v1/projects/{projectId}/tasks/paged → List with filters
PUT /api/v1/projects/{projectId}/tasks/{id}/status → Update status
POST /api/v1/projects/{projectId}/tasks/{id}/assign → Assign to user
```

## Support
For issues or questions, contact: your-email@example.com
",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your-email@example.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // ✅ Enable XML comments (optional but recommended)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        return HealthCheckResult.Healthy("Database is reachable");
    });


var app = builder.Build();

app.UseCors("AllowAngular");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseResponseCaching();

// IMPORTANT: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Make Program accessible to tests
public partial class Program { }