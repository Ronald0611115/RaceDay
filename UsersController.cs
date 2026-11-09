using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Service;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly AzureBlobService _blobService;

        public UsersController(
            RaceDayContext context,
            AzureBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // POST: api/users/me/profile-picture
        [Authorize]
        [HttpPost("me/profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file)
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            if (file == null || file.Length == 0)
                return BadRequest("Please select an image.");

            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
                return BadRequest("Maximum image size is 5 MB.");

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(
                    "Only JPG, PNG and WebP images are allowed.");
            }

            await using var stream = file.OpenReadStream();

            var imageUrl = await _blobService.UploadImageAsync(
                stream,
                file.FileName,
                file.ContentType);

            user.ProfilePictureUrl = imageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
                userId = user.UserId,
                profilePictureUrl = imageUrl
            });
        }

        // GET: api/users/1/profile-picture
        [HttpGet("{userId}/profile-picture")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfilePicture(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            if (string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                return NotFound("This user has no profile picture.");

            try
            {
                var (content, contentType) =
                    await _blobService.DownloadImageAsync(
                        user.ProfilePictureUrl);

                return File(content, contentType);
            }
            catch (Azure.RequestFailedException ex)
                when (ex.Status == 404)
            {
                return NotFound(
                    "Profile picture not found in Azure Storage.");
            }
        }


        // GET: api/users/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        // PUT: api/users/me
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName))
            {
                return BadRequest(
                    "First name and last name cannot be empty.");
            }

            user.FirstName = firstName;
            user.LastName = lastName;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role
            });
        }

    }
}
