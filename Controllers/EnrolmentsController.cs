
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
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/enrolments/mine
        [HttpGet("mine")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<Enrolment>>> GetMyEnrolments()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var participantId))
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == participantId)
                .OrderByDescending(e => e.EnrolmentDate)
                .ToListAsync();

            return Ok(enrolments);
        }

        // POST: api/enrolments/category/1
        [HttpPost("category/{categoryId}")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<Enrolment>> EnrolInCategory(
            int categoryId)
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var participantId))
                return Unauthorized();

            var participantExists = await _context.Users
                .AnyAsync(u => u.UserId == participantId &&
                               u.Role == "Participant");

            if (!participantExists)
                return Unauthorized();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
                return NotFound("Category not found.");

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == category.EventId);

            if (eventItem == null)
                return Conflict("The category's event could not be found.");

            if (eventItem.IsCancelled)
                return Conflict("You cannot enrol in a cancelled event.");

            if (eventItem.EventDate.Date < DateTime.Today)
                return Conflict("You cannot enrol in a past event.");

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantId == participantId &&
                               e.CategoryId == categoryId &&
                               e.Status != "Cancelled");

            if (alreadyEnrolled)
                return Conflict("You are already enrolled in this category.");

            var currentEnrolments = await _context.Enrolments
                .CountAsync(e => e.CategoryId == categoryId &&
                                 e.Status != "Cancelled");

            if (currentEnrolments >= category.MaxParticipants)
                return Conflict("This category is full.");

            var enrolment = new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                EnrolmentDate = DateTime.Now,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyEnrolments),
                null,
                enrolment);
        }

        // GET: api/enrolments/event/1
        [HttpGet("event/{eventId}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<IEnumerable<Enrolment>>> GetEventEnrolments(
            int eventId)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var organiserId))
                return Unauthorized();

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => _context.Categories.Any(
                    c => c.CategoryId == e.CategoryId &&
                         c.EventId == eventId))
                .OrderBy(e => e.EnrolmentDate)
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}
