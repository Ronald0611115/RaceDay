
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;


namespace RaceDay.MVC.Controllers
{
    [Authorize(Roles = "Organiser")]
    public class OrganiserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrganiserController> _logger;

        public OrganiserController(
            IHttpClientFactory httpClientFactory,
            ILogger<OrganiserController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/events");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                response.EnsureSuccessStatusCode();

                var events = await response.Content
                    .ReadFromJsonAsync<List<EventViewModel>>()
                    ?? new List<EventViewModel>();

                if (!int.TryParse(
                   User.FindFirstValue(ClaimTypes.NameIdentifier),
                   out var organiserId))
                {
                    return Unauthorized();
                }

                var myEvents = events
                    .Where(e => e.OrganiserId == organiserId)
                    .OrderBy(e => e.EventDate)
                    .ToList();

                return View(myEvents);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load Organiser dashboard");

                return StatusCode(
                    503,
                    "Organiser dashboard is temporarily unavailable.");
            }
        }


        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View("EventForm", new EventFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View("EventForm", model);

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Post, "api/Events")
            {
                Content = JsonContent.Create(new
                {
                    model.EventName,
                    model.EventType,
                    model.EventDate,
                    model.Location,
                    model.Description
                })
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "", "Unable to create event. Check the event details.");

                    return View("EventForm", model);
                }

                TempData["SuccessMessage"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to create event");
                ModelState.AddModelError("", "Event service unavailable.");
                return View("EventForm", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (id <= 0)
                return BadRequest();

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"api/Events/{id}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                response.EnsureSuccessStatusCode();

                var raceEvent = await response.Content
                    .ReadFromJsonAsync<EventViewModel>();

                if (raceEvent == null)
                    return NotFound();

                if (!int.TryParse(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    out var organiserId))
                    return Unauthorized();

                if (raceEvent.OrganiserId != organiserId)
                    return Forbid();

                return View("EventForm", new EventFormViewModel
                {
                    EventId = raceEvent.EventId,
                    EventName = raceEvent.EventName,
                    EventType = raceEvent.EventType,
                    EventDate = raceEvent.EventDate,
                    Location = raceEvent.Location,
                    Description = raceEvent.Description ?? string.Empty
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load event {EventId}", id);
                return StatusCode(503, "Event service unavailable.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EventFormViewModel model)
        {
            if (model.EventId <= 0)
                return BadRequest();

            if (!ModelState.IsValid)
                return View("EventForm", model);

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Put, $"api/Events/{model.EventId}")
            {
                Content = JsonContent.Create(new
                {
                    model.EventName,
                    model.EventType,
                    model.EventDate,
                    model.Location,
                    model.Description
                })
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Unable to update event.");
                    return View("EventForm", model);
                }

                TempData["SuccessMessage"] = "Event updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to update event");
                ModelState.AddModelError("", "Event service unavailable.");
                return View("EventForm", model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEvent(int id)
        {
            if (id <= 0)
                return BadRequest();

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/Events/{id}/cancel");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event cancelled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Unable to cancel the event.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to cancel event {EventId}", id);

                TempData["ErrorMessage"] =
                    "Event service is temporarily unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(int eventId, IFormFile file)
        {
            if (eventId <= 0)
                return BadRequest();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a banner image.";
                return RedirectToAction(nameof(Index));
            }

            const long maxSize = 5 * 1024 * 1024;

            if (file.Length > maxSize ||
                !new[] { "image/jpeg", "image/png", "image/webp" }
                    .Contains(file.ContentType.ToLowerInvariant()))
            {
                TempData["ErrorMessage"] =
                    "Select a JPG, PNG or WebP image under 5 MB.";
                return RedirectToAction(nameof(Index));
            }

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var imageContent = new StreamContent(stream);

            imageContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            form.Add(imageContent, "file", file.FileName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"api/Events/{eventId}/banner")
            {
                Content = form
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event banner uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Banner upload failed. Check the image and event permissions.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Banner upload failed for event {EventId}", eventId);

                TempData["ErrorMessage"] =
                    "The banner upload service is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public IActionResult CreateCategory(int eventId)
        {
            if (eventId <= 0)
                return BadRequest();

            return View(new CategoryFormViewModel
            {
                EventId = eventId,
                MaxParticipants = 100
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/Categories/event/{model.EventId}")
            {
                Content = JsonContent.Create(new
                {
                    model.CategoryName,
                    model.EntryFee,
                    model.MaxParticipants
                })
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to create category. Check the details.");

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    "Category created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to create category for event {EventId}",
                    model.EventId);

                ModelState.AddModelError(
                    "",
                    "Category service is temporarily unavailable.");

                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (id <= 0)
                return BadRequest();

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"api/Categories/{id}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                response.EnsureSuccessStatusCode();

                var category = await response.Content
                    .ReadFromJsonAsync<CategoryViewModel>();

                if (category == null)
                    return NotFound();

                return View(new CategoryFormViewModel
                {
                    CategoryId = category.CategoryId,
                    EventId = category.EventId,
                    CategoryName = category.CategoryName,
                    EntryFee = category.EntryFee,
                    MaxParticipants = category.MaxParticipants
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load category {CategoryId}", id);
                return StatusCode(503, "Category service unavailable.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
        {
            if (model.CategoryId <= 0 || model.EventId <= 0)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Put, $"api/Categories/{model.CategoryId}")
            {
                Content = JsonContent.Create(new
                {
                    model.CategoryName,
                    model.EntryFee,
                    model.MaxParticipants
                })
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Unable to update category.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Category updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to update category");
                ModelState.AddModelError("", "Category service unavailable.");
                return View(model);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (id <= 0)
                return BadRequest();

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Delete, $"api/Categories/{id}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Category deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Unable to delete category. It may have existing enrolments.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to delete category {CategoryId}", id);
                TempData["ErrorMessage"] = "Category service unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public async Task<IActionResult> ManageCategories(int eventId)
        {
            if (eventId <= 0)
                return BadRequest();

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/Categories/event/{eventId}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                response.EnsureSuccessStatusCode();

                var categories = await response.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>()
                    ?? new List<CategoryViewModel>();

                ViewBag.EventId = eventId;

                return View(categories);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load categories for event {EventId}",
                    eventId);

                return StatusCode(
                    503,
                    "Category service is temporarily unavailable.");
            }
        }


        [HttpGet]
        public async Task<IActionResult> EventEnrolments(int eventId)
        {
            if (eventId <= 0)
                return BadRequest();

            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/enrolments/event/{eventId}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                response.EnsureSuccessStatusCode();

                var enrolments = await response.Content
                    .ReadFromJsonAsync<List<EnrolmentViewModel>>()
                    ?? new List<EnrolmentViewModel>();

                ViewBag.EventId = eventId;

                return View(enrolments);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load enrolments for event {EventId}",
                    eventId);

                return StatusCode(
                    503,
                    "Enrolment service is temporarily unavailable.");
            }
        }




    }
}
