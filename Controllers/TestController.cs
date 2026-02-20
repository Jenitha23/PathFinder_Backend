using Microsoft.AspNetCore.Mvc;

namespace PathFinderBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    // GET: api/test
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "PathFinder API is running successfully!",
            status = "Healthy",
            timestamp = DateTime.Now,
            server = Environment.MachineName,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }

    // GET: api/test/hello
    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok(new
        {
            message = "Hello from PathFinder!",
            timestamp = DateTime.Now
        });
    }

    // GET: api/test/echo/{text}
    [HttpGet("echo/{text}")]
    public IActionResult Echo(string text)
    {
        return Ok(new
        {
            original = text,
            length = text.Length,
            uppercase = text.ToUpper(),
            lowercase = text.ToLower(),
            timestamp = DateTime.Now
        });
    }

    // POST: api/test/ping
    [HttpPost("ping")]
    public IActionResult Ping([FromBody] object data)
    {
        return Ok(new
        {
            message = "Pong!",
            receivedData = data,
            timestamp = DateTime.Now
        });
    }

    // GET: api/test/error-test
    [HttpGet("error-test")]
    public IActionResult ErrorTest()
    {
        throw new Exception("This is a test error!");
    }

    // GET: api/test/status
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            application = "PathFinder Backend",
            version = "1.0.0",
            dotnet_version = Environment.Version.ToString(),
            os = Environment.OSVersion.ToString(),
            processors = Environment.ProcessorCount,
            working_directory = Environment.CurrentDirectory
        });
    }
}