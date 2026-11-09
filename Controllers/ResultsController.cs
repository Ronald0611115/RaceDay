
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
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        // POST: api/results/enrolment/1
        [HttpPost("enrolment/{enrolmentId}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Result>> RecordResult(
            int enrolmentId, ResultRequest request)
        {
            if (request.FinishTime <= TimeSpan.Zero)
                return BadRequest("Finish time must be greater than zero.");

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == enrolment.CategoryId);

            if (category == null)
                return Conflict("The enrolment's category could not be found.");

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == category.EventId);

            if (eventItem == null)
                return Conflict("The category's event could not be found.");

            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var organiserId))
                return Unauthorized();

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

            if (eventItem.IsCancelled)
                return Conflict("Results cannot be recorded for a cancelled event.");

            var resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentId == enrolmentId);

            if (resultExists)
                return Conflict("A result already exists for this enrolment.");

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = request.FinishTime,
                Position = request.Position,
                RecordedAt = DateTime.Now
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return Ok(result);
        }

        // GET: api/results/mine
        [HttpGet("mine")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<Result>>> GetMyResults()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var participantId))
                return Unauthorized();

            var results = await _context.Results
                .Where(r => _context.Enrolments.Any(
                    e => e.EnrolmentId == r.EnrolmentId &&
                         e.ParticipantId == participantId))
                .OrderBy(r => r.Position)
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/enrolment/1
        [HttpGet("enrolment/{enrolmentId}")]
        public async Task<ActionResult<Result>> GetEnrolmentResult(
            int enrolmentId)
        {
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);

            if (result == null)
                return NotFound("Result not found.");

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            var isParticipant = User.IsInRole("Participant");
            var isOrganiser = User.IsInRole("Organiser");

            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
                return Unauthorized();

            if (isParticipant)
            {
                if (enrolment.ParticipantId != userId)
                    return Forbid();

                return Ok(result);
            }

            if (isOrganiser)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(
                        c => c.CategoryId == enrolment.CategoryId);

                if (category == null)
                    return Conflict("The category could not be found.");

                var eventItem = await _context.Events
                    .FirstOrDefaultAsync(
                        e => e.EventId == category.EventId);

                if (eventItem == null)
                    return Conflict("The event could not be found.");

                if (eventItem.OrganiserId != userId)
                    return Forbid();

                return Ok(result);
            }

            return Forbid();
        }
    }
}
