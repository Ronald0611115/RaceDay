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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<Category>>> GetEventCategories(
            int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
                return NotFound("Event not found.");

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound("Category not found.");

            return Ok(category);
        }

        [Authorize(Roles = "Organiser")]
        [HttpPost("event/{eventId}")]
        public async Task<ActionResult<Category>> CreateCategory(
            int eventId, CategoryRequest request)
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

            if (eventItem.IsCancelled)
                return Conflict("Cannot add categories to a cancelled event.");

            var category = new Category
            {
                CategoryName = request.CategoryName.Trim(),
                EntryFee = request.EntryFee,
                MaxParticipants = request.MaxParticipants,
                EventId = eventId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryId },
                category);
        }

        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(
            int id, CategoryRequest request)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound("Category not found.");

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
                return Conflict("Cannot update categories for a cancelled event.");

            category.CategoryName = request.CategoryName.Trim();
            category.EntryFee = request.EntryFee;
            category.MaxParticipants = request.MaxParticipants;

            await _context.SaveChangesAsync();

            return Ok(category);
        }

        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound("Category not found.");

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

            var hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.CategoryId == id);

            var hasRoute = await _context.EventRoutes
                .AnyAsync(r => r.CategoryId == id);

            if (hasEnrolments || hasRoute)
                return Conflict(
                    "Category cannot be deleted because it has enrolments or a route.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
