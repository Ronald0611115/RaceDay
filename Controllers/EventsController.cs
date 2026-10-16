
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/events

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var events = await _context.Events
                .Where(e => e.EventDate >= DateTime.Today && !e.IsCancelled)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/events/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return Ok(eventItem);
        }


        // POST: api/events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(
            CreateEventRequest request)
        {
            var newEvent = new Event
            {
                EventName = request.EventName,
                EventDate = request.EventDate!.Value,
                Location = request.Location,
                Description = request.Description,

                // Temporary for local testing only.
                // Replace with the authenticated organiser's ID
                // before using this endpoint in the final API.
                OrganiserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = newEvent.EventId },
                newEvent);
        }


        // PUT: api/events/2
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(
            int id, CreateEventRequest request)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            var organiserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

            eventItem.EventName = request.EventName;
            eventItem.EventDate = request.EventDate!.Value;
            eventItem.Location = request.Location;
            eventItem.Description = request.Description;

            await _context.SaveChangesAsync();

            return Ok(eventItem);
        }


        // PATCH: api/events/{id}/cancel
        [Authorize(Roles = "Organiser")]
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelEvent(int id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            var organiserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (eventItem.OrganiserId != organiserId)
                return Forbid();

            if (eventItem.IsCancelled)
            {
                return BadRequest("This event is already cancelled.");
            }

            eventItem.IsCancelled = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event cancelled successfully.",
                eventId = eventItem.EventId,
                eventName = eventItem.EventName,
                isCancelled = eventItem.IsCancelled
            });
        }

    }
}
