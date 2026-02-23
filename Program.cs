using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register your app services (ADO.NET + Repos + Services)
builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<StudentRepository>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtTokenService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key missing in appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PathFinder";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PathFinderUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS should come before auth (good practice)
app.UseCors("AllowAll");

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root endpoint
app.MapGet("/", () => "PathFinder API is running!");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.Now }));

app.Run();