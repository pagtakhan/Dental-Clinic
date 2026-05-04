using DentalClinic.Data;
using DentalClinic.Models;
using DentalClinic.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DentalClinic.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly IHttpContextAccessor _http;

        public AccountController(AppDbContext db, AuditService audit, IHttpContextAccessor http)
        {
            _db = db;
            _audit = audit;
            _http = http;
        }

        private string GetIp() =>
            _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ── GET /Account/Login ─────────────────────────────────
        public IActionResult Login() =>
            User.Identity?.IsAuthenticated == true ? RedirectToDashboard() : View();

        // ── POST /Account/Login ────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Lockout check
            if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
            {
                ModelState.AddModelError("", $"Account locked. Try again after {user.LockoutUntil:hh:mm tt}.");
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                    ModelState.AddModelError("", "Too many failed attempts. Account locked for 15 minutes.");
                }
                else
                {
                    ModelState.AddModelError("", $"Invalid username or password. ({5 - user.FailedLoginAttempts} attempts left)");
                }
                await _db.SaveChangesAsync();
                return View(model);
            }

            // Success — reset counters
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
                new(ClaimTypes.Email, user.Email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });

            await _audit.LogAsync(user.UserId, "User logged in", "Users", user.UserId, GetIp());
            return RedirectToDashboard(user.Role);
        }

        // ── GET /Account/Register ──────────────────────────────
        public IActionResult Register() => View();

        // ── POST /Account/Register ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already taken.");
                return View(model);
            }
            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Patient",
                IsActive = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var patient = new Patient
            {
                UserId = user.UserId,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth,
                ContactNumber = model.ContactNumber,
                Address = model.Address,
                Allergies = model.Allergies,
                MedicalHistory = model.MedicalHistory
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(user.UserId, "New patient registered", "Users", user.UserId, GetIp());
            TempData["Success"] = "Registration successful! Please log in.";
            return RedirectToAction("Login");
        }

        // ── GET /Account/Logout ────────────────────────────────
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _audit.LogAsync(uid, "User logged out", "Users", uid, GetIp());
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToDashboard(string? role = null)
        {
            role ??= User.FindFirstValue(ClaimTypes.Role);
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Dentist" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
