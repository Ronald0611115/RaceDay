using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Service;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(Roles = "Organiser")]
    public class EventImagesController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly AzureBlobService _blobService;

        public EventImagesController(
            RaceDayContext context,
            AzureBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // POST: api/events/1/banner
        [HttpPost("{eventId}/banner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBanner(
   int eventId,
   IFormFile file)


        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var organiserId))
            {
                return Unauthorized();
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

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

            eventItem.BannerImageUrl = imageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event banner uploaded successfully.",
                eventId = eventItem.EventId,
                bannerImageUrl = imageUrl
            });
        }

        // GET: api/events/1/banner
        [HttpGet("{eventId}/banner")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBanner(int eventId)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (string.IsNullOrWhiteSpace(eventItem.BannerImageUrl))
                return NotFound("This event has no banner.");

            try
            {
                var (content, contentType) =
                    await _blobService.DownloadImageAsync(
                        eventItem.BannerImageUrl);

                return File(content, contentType);
            }
            catch (Azure.RequestFailedException ex)
                when (ex.Status == 404)
            {
                return NotFound("Banner image not found in Azure Storage.");
            }
        }
    }
}