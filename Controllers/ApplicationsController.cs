using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/applications")]
    [Authorize(Roles = "STUDENT")]
    public class ApplicationsController : ControllerBase
    {
        private readonly Db _db;

        public ApplicationsController(Db db)
        {
            _db = db;
        }

        /// <summary>
        /// POST /api/applications
        /// Allows a logged-in student to apply for a job.
        /// Validates profile completeness (skills + CV) and prevents duplicate applications.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] ApplyJobRequest req)
        {
            // 1. Extract student ID from JWT
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized(new { message = "Invalid token: missing userId." });

            // 2. Validate profile completeness (skills + CV must be present)
            var profileRepo = new StudentProfileRepository(_db);
            await profileRepo.EnsureTableAndColumnsAsync();

            var profile = await profileRepo.GetStudentProfileAsync(studentId.Value);
            if (profile == null)
                return BadRequest(new
                {
                    message = "Student profile not found. Please create your profile first.",
                    code = "no_profile"
                });

            if (string.IsNullOrWhiteSpace(profile.Skills) && string.IsNullOrWhiteSpace(profile.TechnicalSkills))
                return BadRequest(new
                {
                    message = "Your profile is incomplete. Please add your skills before applying.",
                    code = "incomplete_profile",
                    missingFields = new[] { "skills" }
                });

            if (string.IsNullOrWhiteSpace(profile.CvUrl))
                return BadRequest(new
                {
                    message = "Your profile is incomplete. Please upload your CV before applying.",
                    code = "incomplete_profile",
                    missingFields = new[] { "cv" }
                });

            // 3. Verify the job exists
            var jobRepo = new JobRepository(_db);
            await jobRepo.EnsureTableAndIndexesAsync();
            var job = await jobRepo.GetJobByIdAsync(req.JobId);
            if (job == null)
                return NotFound(new
                {
                    message = $"Job with ID {req.JobId} does not exist.",
                    code = "job_not_found"
                });

            // 4. Ensure applications table exists
            var appRepo = new ApplicationRepository(_db);
            await appRepo.EnsureTableAndConstraintsAsync();

            // 5. Prevent duplicate applications
            var alreadyApplied = await appRepo.HasStudentAppliedAsync(studentId.Value, req.JobId);
            if (alreadyApplied)
                return Conflict(new
                {
                    message = "You have already applied for this job.",
                    code = "duplicate_application"
                });

            // 6. Save application with status = "Pending"
            var applicationId = await appRepo.ApplyForJobAsync(
                studentId.Value,
                req.JobId,
                req.CoverLetter?.Trim()
            );

            return StatusCode(201, new
            {
                message = "Application submitted successfully.",
                applicationId,
                status = "Pending"
            });
        }

        private int? GetStudentIdFromToken()
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr))
            {
                userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
            }
            if (int.TryParse(userIdStr, out var id)) return id;
            return null;
        }
    }
}
