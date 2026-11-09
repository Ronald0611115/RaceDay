
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace RaceDay.MVC.Controllers
{
    [Authorize(Roles = "Participant")]
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IHttpClientFactory httpClientFactory,
            ILogger<ProfileController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient CreateApiClient()
        {
            var client = _httpClientFactory
                .CreateClient("RaceDayAPI");

            var token = HttpContext.Session
                .GetString("RaceDayJwt");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("RaceDayJwt")))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = CreateApiClient();

            try
            {
                using var response = await client.GetAsync("api/users/me");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                response.EnsureSuccessStatusCode();

                var profile = await response.Content
                    .ReadFromJsonAsync<ProfileViewModel>();

                if (profile == null)
                    return NotFound();

                return View(profile);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load profile");

                return StatusCode(503,
                    "Profile service is temporarily unavailable.");
            }
        }

        // POST: /Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            ModelState.Remove(nameof(ProfileViewModel.Email));
            ModelState.Remove(nameof(ProfileViewModel.Role));
            ModelState.Remove(nameof(ProfileViewModel.ProfilePictureUrl));

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Please enter a valid first name and last name.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("RaceDayJwt")))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = CreateApiClient();

            try
            {
                using var response = await client.PutAsJsonAsync(
                    "api/users/me",
                    new
                    {
                        firstName = model.FirstName.Trim(),
                        lastName = model.LastName.Trim()
                    });

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Profile updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Unable to update your profile.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Profile update failed");

                TempData["ErrorMessage"] =
                    "Profile service is temporarily unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Profile/UploadPicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Select an image.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] =
                    "Maximum image size is 5 MB.";

                return RedirectToAction(nameof(Index));
            }

            var allowedTypes = new[]
            {
                "image/jpeg", "image/png", "image/webp"
            };

            if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                TempData["ErrorMessage"] =
                    "Only JPG, PNG and WebP images are allowed.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("RaceDayJwt")))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = CreateApiClient();

            try
            {
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream();

                using var imageContent = new StreamContent(stream);

                imageContent.Headers.ContentType =
                    new MediaTypeHeaderValue(file.ContentType);

                content.Add(imageContent, "file", file.FileName);

                using var response = await client.PostAsync(
                    "api/users/me/profile-picture", content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                TempData[response.IsSuccessStatusCode
                    ? "SuccessMessage"
                    : "ErrorMessage"] = response.IsSuccessStatusCode
                    ? "Profile picture uploaded successfully."
                    : "Unable to upload profile picture.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Profile picture upload failed");

                TempData["ErrorMessage"] =
                    "Image upload service is temporarily unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Profile/Picture
        [HttpGet]
        public async Task<IActionResult> Picture()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }

            var client = CreateApiClient();

            try
            {
                using var response = await client.GetAsync(
                    $"api/users/{userId}/profile-picture");

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var bytes = await response.Content
                    .ReadAsByteArrayAsync();

                var contentType = response.Content.Headers
                    .ContentType?.MediaType ?? "image/jpeg";

                return File(bytes, contentType);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Unable to load profile picture");
                return NotFound();
            }
        }
    }
}
