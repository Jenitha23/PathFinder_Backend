using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company/profile")]
    [Authorize(Roles = "COMPANY")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly CompanyProfileRepository _profileRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly BlobService _blobService;

        public CompanyProfileController(
            CompanyProfileRepository profileRepo,
            CompanyRepository companyRepo,
            BlobService blobService)
        {
            _profileRepo = profileRepo;
            _companyRepo = companyRepo;
            _blobService = blobService;
        }

        /// <summary>
        /// GET /api/company/profile
        /// Retrieves the current company's profile details.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetCurrentCompanyId(out var companyId))
                return Unauthorized(new { message = "Invalid token: missing userId." });

            var company = await _profileRepo.GetCompanyProfileAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            var response = new CompanyProfileResponse
            {
                Id = company.Id,
                CompanyName = company.CompanyName,
                Email = company.Email,
                Description = company.Description,
                Industry = company.Industry,
                Website = company.Website,
                Location = company.Location,
                Phone = company.Phone,
                LogoUrl = company.LogoUrl,
                Status = company.Status,
                CreatedAt = company.CreatedAt
            };

            return Ok(response);
        }

        /// <summary>
        /// PUT /api/company/update-profile
        /// Updates company profile - handles both JSON and form-data
        /// </summary>
        [HttpPut("update-profile")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UpdateProfile()
        {
            if (!TryGetCurrentCompanyId(out var companyId))
                return Unauthorized(new { message = "Invalid token: missing userId." });

            var existingCompany = await _profileRepo.GetCompanyProfileAsync(companyId);
            if (existingCompany == null)
                return NotFound(new { message = "Company not found." });

            string companyName = existingCompany.CompanyName;
            string email = existingCompany.Email;
            string? description = existingCompany.Description;
            string? industry = existingCompany.Industry;
            string? website = existingCompany.Website;
            string? location = existingCompany.Location;
            string? phone = existingCompany.Phone;
            bool removeLogo = false;
            string? savedLogoUrl = null;
            string? oldLogoUrl = existingCompany.LogoUrl;

            // Check if request is form-data (file upload) or JSON
            if (Request.HasFormContentType)
            {
                // Handle form-data (with possible file upload)
                var form = await Request.ReadFormAsync();
                
                companyName = form["CompanyName"].ToString();
                email = form["Email"].ToString();
                description = form["Description"].ToString();
                industry = form["Industry"].ToString();
                website = form["Website"].ToString();
                location = form["Location"].ToString();
                phone = form["Phone"].ToString();
                removeLogo = form["RemoveLogo"].ToString().ToLower() == "true";
                
                // Handle file upload
                var logoFile = form.Files.GetFile("LogoFile");
                if (logoFile != null && logoFile.Length > 0)
                {
                    try
                    {
                        savedLogoUrl = await _blobService.UploadCompanyLogoAsync(logoFile, companyId);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message, field = "logoFile" });
                    }
                    catch (Exception)
                    {
                        return StatusCode(500, new { message = "Failed to upload logo. Please try again." });
                    }
                }
            }
            else
            {
                // Handle JSON request
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                
                try
                {
                    var jsonDoc = JsonDocument.Parse(body);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("companyName", out var nameElement))
                        companyName = nameElement.GetString() ?? existingCompany.CompanyName;
                        
                    if (root.TryGetProperty("email", out var emailElement))
                        email = emailElement.GetString() ?? existingCompany.Email;
                        
                    if (root.TryGetProperty("description", out var descElement))
                        description = descElement.GetString();
                        
                    if (root.TryGetProperty("industry", out var industryElement))
                        industry = industryElement.GetString();
                        
                    if (root.TryGetProperty("website", out var websiteElement))
                        website = websiteElement.GetString();
                        
                    if (root.TryGetProperty("location", out var locationElement))
                        location = locationElement.GetString();
                        
                    if (root.TryGetProperty("phone", out var phoneElement))
                        phone = phoneElement.GetString();
                        
                    if (root.TryGetProperty("removeLogo", out var removeElement))
                        removeLogo = removeElement.GetBoolean();
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "Invalid JSON format." });
                }
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(companyName))
                return BadRequest(new { message = "Company name is required.", field = "companyName" });
                
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required.", field = "email" });
                
            // Validate email format
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email) throw new Exception();
            }
            catch
            {
                return BadRequest(new { message = "Invalid email format.", field = "email" });
            }
            
            email = email.Trim().ToLowerInvariant();

            // Validate email uniqueness
            var emailOwner = await _companyRepo.GetByEmailAsync(email);
            if (emailOwner != null && emailOwner.Id != companyId)
                return Conflict(new { message = "Email already registered by another company." });

            // Update profile in database
            var updated = await _profileRepo.UpdateCompanyProfileAsync(
                companyId,
                companyName.Trim(),
                email,
                description?.Trim(),
                industry?.Trim(),
                website?.Trim(),
                location?.Trim(),
                phone?.Trim(),
                savedLogoUrl,
                removeLogo
            );

            if (!updated)
                return StatusCode(500, new { message = "Failed to update profile." });

            // Delete old logo if replaced or removed
            if (removeLogo && !string.IsNullOrWhiteSpace(oldLogoUrl))
            {
                await _blobService.DeleteCompanyLogoAsync(companyId, oldLogoUrl);
            }
            else if (savedLogoUrl != null && !string.IsNullOrWhiteSpace(oldLogoUrl))
            {
                await _blobService.DeleteCompanyLogoAsync(companyId, oldLogoUrl);
            }

            // Return updated profile
            var updatedCompany = await _profileRepo.GetCompanyProfileAsync(companyId);
            
            return Ok(new
            {
                message = "Profile updated successfully.",
                profile = new CompanyProfileResponse
                {
                    Id = updatedCompany!.Id,
                    CompanyName = updatedCompany.CompanyName,
                    Email = updatedCompany.Email,
                    Description = updatedCompany.Description,
                    Industry = updatedCompany.Industry,
                    Website = updatedCompany.Website,
                    Location = updatedCompany.Location,
                    Phone = updatedCompany.Phone,
                    LogoUrl = updatedCompany.LogoUrl,
                    Status = updatedCompany.Status,
                    CreatedAt = updatedCompany.CreatedAt
                }
            });
        }

        /// <summary>
        /// DELETE /api/company/profile/logo
        /// Removes the current company's logo.
        /// </summary>
        [HttpDelete("logo")]
        public async Task<IActionResult> RemoveLogo()
        {
            if (!TryGetCurrentCompanyId(out var companyId))
                return Unauthorized(new { message = "Invalid token: missing userId." });

            var company = await _profileRepo.GetCompanyProfileAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            var oldLogoUrl = company.LogoUrl;
            
            if (string.IsNullOrWhiteSpace(oldLogoUrl))
                return Ok(new { message = "No logo to remove." });

            // Update database to remove logo URL
            var updated = await _profileRepo.UpdateCompanyLogoAsync(companyId, null);

            if (!updated)
                return StatusCode(500, new { message = "Failed to remove logo." });

            // Delete the actual file from local storage
            await _blobService.DeleteCompanyLogoAsync(companyId, oldLogoUrl);

            return Ok(new { message = "Logo removed successfully." });
        }

        private bool TryGetCurrentCompanyId(out int companyId)
        {
            companyId = 0;

            var userId = User.FindFirst("userId")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out companyId);
        }
    }
}