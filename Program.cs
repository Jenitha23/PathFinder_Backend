using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MVC controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register app services (ADO.NET + Repositories + Services)
// Db: connection provider
builder.Services.AddSingleton<Db>();

// Repositories: DB access
builder.Services.AddScoped<StudentRepository>();
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<AdminRepository>();

// Services: stateless helpers (hashing, token creation, revocation tracking)
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtTokenService>();

// Token revocation is stored in-memory (sufficient for single-instance demo).
builder.Services.AddSingleton<TokenRevocationService>();

// JWT Authentication settings
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key missing in appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PathFinder";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PathFinderUsers";

// Configure JWT validation middleware
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validate issuer/audience/signature/lifetime for security
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Allow small server/client time mismatch
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Custom logic after token signature is validated:
        // - check whether token has been revoked (logout)
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // JTI claim uniquely identifies the token.
                // Your JwtTokenService uses JwtRegisteredClaimNames.Jti ("jti")
                var jti = context.Principal?.FindFirst("jti")?.Value;

                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Token missing jti.");
                    return Task.CompletedTask;
                }

                // If token is revoked, block access even if not expired
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

// Add CORS (AllowAll policy is fine for development/demo)
// For production, lock down origins and headers.
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

// Swagger UI only in Development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS should be before auth middleware
app.UseCors("AllowAll");

// Authentication MUST run before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root endpoint
app.MapGet("/", () => "PathFinder API is running!");

// Health endpoint for monitoring/testing
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.Now }));

// Seed default admin if enabled (idempotent)
// Ensures at least one admin exists for login testing.
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

        // Hash password before inserting (never store plain password)
        await adminRepo.EnsureSeedAdminAsync(seedFullName, seedEmail, pwd.Hash(seedPassword));
    }
}

app.Run();