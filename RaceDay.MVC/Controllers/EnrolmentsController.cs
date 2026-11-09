
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers
{
    [Authorize(Roles = "Participant")]
    public class EnrolmentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EnrolmentsController> _logger;

        public EnrolmentsController(
            IHttpClientFactory httpClientFactory,
            ILogger<EnrolmentsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // POST: Enrolments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int categoryId,
            int eventId)
        {
            if (categoryId <= 0 || eventId <= 0)
            {
                return BadRequest();
            }

            var token = HttpContext.Session
                .GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/enrolments/category/{categoryId}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "You have successfully enrolled!";
                }
                else if (response.StatusCode ==
                         HttpStatusCode.Unauthorized)
                {
                    TempData["ErrorMessage"] =
                        "Your API session has expired. Please log in again.";
                }
                else if (response.StatusCode ==
                         HttpStatusCode.Forbidden)
                {
                    TempData["ErrorMessage"] =
                        "You do not have permission to enrol.";
                }
                else
                {
                    _logger.LogWarning(
                        "Enrolment failed with HTTP {StatusCode}",
                        (int)response.StatusCode);

                    TempData["ErrorMessage"] =
                        "Enrolment could not be completed. " +
                        "You may already be enrolled, " +
                        "or the category may be full.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to contact enrolment API");

                TempData["ErrorMessage"] =
                    "The enrolment service is temporarily unavailable.";
            }

            return RedirectToAction(
                "Details",
                "Home",
                new { id = eventId });
        }

        // GET: Enrolments/MyEnrolments
        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            var token = HttpContext.Session
                .GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/enrolments/mine");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request);

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    TempData["ErrorMessage"] =
                        "Your session has expired. Please log in again.";

                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                response.EnsureSuccessStatusCode();


                var enrolments = await response.Content
                    .ReadFromJsonAsync<List<EnrolmentViewModel>>()
                    ?? new List<EnrolmentViewModel>();

                // Retrieve the category and event details for each enrolment
                foreach (var enrolment in enrolments)
                {
                    try
                    {
                        // Get category information
                        var category = await client
                            .GetFromJsonAsync<CategoryViewModel>(
                                $"api/Categories/{enrolment.CategoryId}");

                        if (category == null)
                        {
                            continue;
                        }

                        enrolment.CategoryName = category.CategoryName;
                        enrolment.EventId = category.EventId;

                        // Get event information
                        var raceEvent = await client
                            .GetFromJsonAsync<EventViewModel>(
                                $"api/events/{category.EventId}");

                        if (raceEvent != null)
                        {
                            enrolment.EventName = raceEvent.EventName;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Could not load event/category details for enrolment {EnrolmentId}",
                            enrolment.EnrolmentId);
                    }
                }

                return View(enrolments);

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to retrieve Participant enrolments");

                return StatusCode(
                    503,
                    "Enrolment history is temporarily unavailable.");
            }
        }
    }
}
