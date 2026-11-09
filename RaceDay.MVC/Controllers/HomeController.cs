
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Models;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IHttpClientFactory httpClientFactory,
            ILogger<HomeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            try
            {
                var events = await client.GetFromJsonAsync<
                    List<EventViewModel>>("api/events");

                return View(events ?? new List<EventViewModel>());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "Unable to retrieve events from RaceDay API.");

                ViewBag.ErrorMessage =
                    "Events are temporarily unavailable.";

                return View(new List<EventViewModel>());
            }
        }


        [HttpGet]

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            try
            {
                // Step 1: Retrieve event details
                var response = await client.GetAsync($"api/events/{id}");

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                response.EnsureSuccessStatusCode();

                var raceEvent = await response.Content
                    .ReadFromJsonAsync<EventViewModel>();

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Step 2: Retrieve categories for this event
                var categories = await client
                    .GetFromJsonAsync<List<CategoryViewModel>>(
                        $"api/Categories/event/{id}");

                // Step 3: Pass categories to the Razor view
                ViewBag.Categories =
                    categories ?? new List<CategoryViewModel>();

                // Step 4: Display the event and its categories
                return View(raceEvent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to retrieve event or categories for event {EventId}",
                    id);

                return StatusCode(
                    503,
                    "Event details are temporarily unavailable.");
            }
        }


        public IActionResult Privacy()
        {
            return View();
        }
    }
}
