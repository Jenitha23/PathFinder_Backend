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
builder.Services.AddSingleton<TokenRevocationService>();
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<AdminRepository>();

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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var jti = context.Principal?.FindFirst("jti")?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Token missing jti.");
                    return Task.CompletedTask;
                }

                var revocationService = context.HttpContext.RequestServices.GetRequiredService<TokenRevocationService>();
                if (revocationService.IsRevoked(jti))
                {
                    context.Fail("Token has been revoked.");
                }

                return Task.CompletedTask;
            }
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

// Seed a default admin if it does not exist yet.
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminRepo = scope.ServiceProvider.GetRequiredService<AdminRepository>();
    var pwd = scope.ServiceProvider.GetRequiredService<PasswordService>();

    var seedEnabled = config.GetValue("AdminSeed:Enabled", true);
    if (seedEnabled)
    {
        var seedFullName = (config["AdminSeed:FullName"] ?? "System Admin").Trim();
        var seedEmail = (config["AdminSeed:Email"] ?? "admin@pathfinder.com").Trim().ToLowerInvariant();
        var seedPassword = config["AdminSeed:Password"] ?? "Admin@123";

        await adminRepo.EnsureSeedAdminAsync(seedFullName, seedEmail, pwd.Hash(seedPassword));
    }
}

app.Run();
