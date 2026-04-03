using DbUp;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Starting database migration...");

// Detect environment (Development / Production)
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

Console.WriteLine($"Environment: {environment}");

// Build configuration (appsettings + environment variables)
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Get connection string
var connectionString =
    config.GetConnectionString("DefaultConnection") ??
    config["ConnectionStrings:DefaultConnection"] ??
    config["DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ ERROR: Database connection string is missing.");
    Console.ResetColor();
    Environment.Exit(1);
}

Console.WriteLine("✅ Connection string loaded");

// Path to migration scripts
var scriptsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");

if (!Directory.Exists(scriptsPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ ERROR: Migrations folder not found: {scriptsPath}");
    Console.ResetColor();
    Environment.Exit(1);
}

Console.WriteLine($"📂 Using migration scripts from: {scriptsPath}");

// Configure DbUp
var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsFromFileSystem(scriptsPath)
    .LogToConsole()
    .WithTransaction()
    .Build();

// Run migrations
var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ Database migration FAILED");
    Console.WriteLine(result.Error);
    Console.ResetColor();
    Environment.Exit(1);
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("🎉 Database migration completed successfully!");
Console.ResetColor();