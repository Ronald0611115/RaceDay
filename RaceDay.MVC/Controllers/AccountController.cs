
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Security.Claims;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IHttpClientFactory httpClientFactory,
            ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory
                .CreateClient("RaceDayAPI");

            try
            {
                var response = await client.PostAsJsonAsync(
                    "api/auth/login",
                    new
                    {
                        model.Email,
                        model.Password
                    });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "RaceDay API login returned HTTP {StatusCode}",
                        (int)response.StatusCode);

                    ModelState.AddModelError(
                        "",
                        response.StatusCode ==
                            System.Net.HttpStatusCode.Unauthorized
                            ? "Invalid email or password."
                            : "Login service returned an error. Please try again.");

                    return View(model);
                }

                var loginResponse =
                    await response.Content
                        .ReadFromJsonAsync<LoginResponse>();

                if (loginResponse == null ||
                    string.IsNullOrWhiteSpace(loginResponse.Token) ||
                    loginResponse.User == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The login response was invalid.");

                    return View(model);
                }

                var user = loginResponse.User;

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.UserId.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        user.Email),

                    new Claim(
                        ClaimTypes.Role,
                        user.Role),

                    new Claim(
                        "FirstName",
                        user.FirstName)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                // Clear any previous session data.
                HttpContext.Session.Clear();

                // Keep the API JWT on the server.
                HttpContext.Session.SetString(
                    "RaceDayJwt",
                    loginResponse.Token);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc =
                            DateTimeOffset.UtcNow.AddHours(2),
                        AllowRefresh = false
                    });

                return RedirectToAction(
                    "Index",
                    "Home");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "RaceDay API login request failed.");

                ModelState.AddModelError(
                    "",
                    "Unable to contact the login service.");

                return View(model);
            }
        }


        [HttpGet]
        [Authorize]
        public IActionResult TestSession()
        {
            return Ok(new
            {
                authenticated = User.Identity?.IsAuthenticated,
                email = User.Identity?.Name,
                role = User.FindFirst(
                    System.Security.Claims.ClaimTypes.Role)?.Value,
                hasApiToken = !string.IsNullOrWhiteSpace(
                    HttpContext.Session.GetString("RaceDayJwt"))
            });
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return RedirectToAction(
                "Index",
                "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
