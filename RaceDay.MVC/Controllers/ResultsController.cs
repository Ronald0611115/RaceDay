
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers
{
    [Authorize(Roles = "Participant")]
    public class ResultsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(
            IHttpClientFactory httpClientFactory,
            ILogger<ResultsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var token = HttpContext.Session.GetString("RaceDayJwt");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            using var request = new HttpRequestMessage(
                HttpMethod.Get, "api/Results/mine");

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

                var results = await response.Content
                    .ReadFromJsonAsync<List<ResultViewModel>>();

                return View(results ?? new List<ResultViewModel>());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load participant results");

                return StatusCode(503,
                    "Results are temporarily unavailable.");
            }
        }
    }
}
