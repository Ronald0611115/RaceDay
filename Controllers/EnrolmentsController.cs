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
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        [Authorize(Roles = "Participant")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            if (!TryGetUserId(out var participantId))
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == participantId)
                .OrderByDescending(e => e.EnrolmentDate)
                .ToListAsync();

            return Ok(enrolments);
        }

        [Authorize(Roles = "Participant")]
        [HttpPost("category/{categoryId}")]
        public async Task<IActionResult> EnrolInCategory(int categoryId)
        {
            if (!TryGetUserId(out var participantId))
                return Unauthorized();

            var participant = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == participantId);

            if (participant == null || participant.Role != "Participant")
                return Forbid();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
                return NotFound("Category not found.");

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == category.EventId);

            if (eventItem == null)
                return Conflict("The category's event could not be found.");

            if (eventItem.IsCancelled)
                return Conflict("Cannot enrol in a cancelled event.");

            if (eventItem.EventDate.Date < DateTime.UtcNow.Date)
                return Conflict("Cannot enrol in an event that has already passed.");

            var existingEnrolment = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantId == participantId &&
                    e.CategoryId == categoryId &&
                    e.Status != "Cancelled");

            if (existingEnrolment)
                return Conflict("You are already enrolled in this category.");

            var currentEnrolments = await _context.Enrolments
                .CountAsync(e =>
                    e.CategoryId == categoryId &&
                    e.Status != "Cancelled");

            if (currentEnrolments >= category.MaxParticipants)
                return Conflict("This category has reached its participant limit.");

            var enrolment = new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(enrolment);
        }

        [Authorize(Roles = "Organiser")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            if (!TryGetUserId(out var organiserId))
                return Unauthorized();

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => _context.Categories.Any(c =>
                    c.CategoryId == e.CategoryId &&
                    c.EventId == eventId))
                .OrderBy(e => e.EnrolmentDate)
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}
