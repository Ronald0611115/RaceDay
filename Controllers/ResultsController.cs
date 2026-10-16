using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        [Authorize(Roles = "Organiser")]
        [HttpPost("enrolment/{enrolmentId}")]
        public async Task<IActionResult> RecordResult(
            int enrolmentId, ResultRequest request)
        {
            if (!TryGetUserId(out var organiserId))
                return Unauthorized();

            if (request.FinishTime <= TimeSpan.Zero)
                return BadRequest("Finish time must be greater than zero.");

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            if (enrolment.Status == "Cancelled")
                return Conflict("Cannot record a result for a cancelled enrolment.");

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == enrolment.CategoryId);

            if (category == null)
                return Conflict("The enrolment's category could not be found.");

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == category.EventId);

            if (eventItem == null)
                return Conflict("The category's event could not be found.");

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

            var existingResult = await _context.Results
                .AnyAsync(r => r.EnrolmentId == enrolmentId);

            if (existingResult)
                return Conflict("A result has already been recorded for this enrolment.");

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = request.FinishTime,
                Position = request.Position,
                RecordedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return Ok(result);
        }

        [Authorize(Roles = "Participant")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyResults()
        {
            if (!TryGetUserId(out var participantId))
                return Unauthorized();

            var results = await _context.Results
                .Where(r => _context.Enrolments.Any(e =>
                    e.EnrolmentId == r.EnrolmentId &&
                    e.ParticipantId == participantId))
                .OrderBy(r => r.Position)
                .ToListAsync();

            return Ok(results);
        }

        [Authorize]
        [HttpGet("enrolment/{enrolmentId}")]
        public async Task<IActionResult> GetEnrolmentResult(int enrolmentId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Participant")
            {
                if (enrolment.ParticipantId != userId)
                    return Forbid();
            }
            else if (userRole == "Organiser")
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryId == enrolment.CategoryId);

                if (category == null)
                    return Conflict("The enrolment's category could not be found.");

                var eventItem = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventId == category.EventId);

                if (eventItem == null)
                    return Conflict("The category's event could not be found.");

                if (eventItem.OrganiserId != userId)
                    return Forbid();
            }
            else
            {
                return Forbid();
            }

            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);

            if (result == null)
                return NotFound("No result has been recorded for this enrolment.");

            return Ok(result);
        }
    }
}
