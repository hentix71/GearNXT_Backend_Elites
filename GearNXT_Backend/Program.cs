using GearNXT_Backend.Data;
using GearNXT_Backend.Helpers;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DotNetEnv;
using System.Text;
using System.Text.Json.Serialization;
using GearNXT_Backend.Services.Interfaces;

// Load environment variables from .env file
Env.Load();


var builder = WebApplication.CreateBuilder(args);


// For Structural import
builder.Configuration.AddEnvironmentVariables();


// For PostgreSQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));    


// For JWT Authentication

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,          // check token was made by GearNXT_API
        ValidateAudience = true,        // check token is meant for GearNXT_Client
        ValidateLifetime = true,        // check token hasn't expired
        ValidateIssuerSigningKey = true,// check signature is valid
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)) // uses your secret key to verify
    };
});

builder.Services.AddAuthorization();

// ============================================
// AutoMapper
// ============================================
builder.Services.AddAutoMapper(typeof(Program));

// ============================================
// JwtHelper — Dependency Injection
// ============================================
builder.Services.AddScoped<JwtHelper>();
// Email service (development logger)
builder.Services.AddScoped<EmailService>();

// ============================================
// Controllers
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()); // This converts enums to strings
    });

// ============================================
// Swagger with JWT Support
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GearNXT API",
        Version = "v1",
        Description = "Vehicle Parts Selling and Inventory Management System"
    });

    // Add JWT authorization button in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token here}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});


// ============================================
// Services — Dependency Injection
// ============================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<PartsService>();
builder.Services.AddScoped<PurchaseInvoiceService>();
builder.Services.AddScoped<LowStockNotifier>();
builder.Services.AddScoped<NotificationService>();


// ============================================
// CORS — allows frontend to call the API
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ============================================
// Middleware Pipeline
// ============================================
app.UseMiddleware<GearNXT_Backend.Middleware.ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication(); // must be before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();