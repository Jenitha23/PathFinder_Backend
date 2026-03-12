using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Repositories;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly Db _db;

        public JobsController(Db db)
        {
            _db = db;
        }

        // GET /api/jobs?page=1&pageSize=10&keyword=java&title=developer&company=wso2&location=Colombo&type=Internship&category=Software Engineering
        [HttpGet]
        public async Task<IActionResult> GetJobs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            [FromQuery] string? title = null,
            [FromQuery] string? company = null,
            [FromQuery] string? location = null,
            [FromQuery] string? type = null,
            [FromQuery] string? category = null)
        {
            var repo = new JobRepository(_db);
            await repo.EnsureTableAndIndexesAsync();

            var result = await repo.GetJobsAsync(
                page,
                pageSize,
                keyword,
                title,
                company,
                location,
                type,
                category
            );

            return Ok(result);
        }

        // GET /api/jobs/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var repo = new JobRepository(_db);
            await repo.EnsureTableAndIndexesAsync();

            var job = await repo.GetJobByIdAsync(id);
            if (job == null)
                return NotFound(new { message = "Job not found." });

            return Ok(job);
        }
    }
}