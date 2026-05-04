using DentalClinic.Data;
using DentalClinic.Models;
using DentalClinic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DentalClinic.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly IHttpContextAccessor _http;

        public AdminController(AppDbContext db, AuditService audit, IHttpContextAccessor http)
        {
            _db = db; _audit = audit; _http = http;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetIp() => _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ── Dashboard ──────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalPatients = await _db.Patients.CountAsync(),
                TotalDentists = await _db.Dentists.CountAsync(),
                TodayAppointments = await _db.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
                PendingBills = await _db.Billings
                    .CountAsync(b => b.PaymentStatus != "Paid"),
                RecentAppointments = await _db.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Dentist)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(10)
                    .ToListAsync()
            };
            return View(vm);
        }

        // ── Users ──────────────────────────────────────────────
        public async Task<IActionResult> Users() =>
            View(await _db.Users.OrderBy(u => u.Role).ThenBy(u => u.Username).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetUserId(), $"User '{user.Username}' {(user.IsActive ? "activated" : "deactivated")}", "Users", id, GetIp());
            TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction("Users");
        }

        // ── Add Dentist ────────────────────────────────────────
        [HttpGet] public IActionResult AddDentist() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDentist(string username, string email, string password,
            string fullName, string specialization, string contactNumber)
        {
            if (await _db.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("", "Username already taken.");
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Dentist",
                IsActive = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _db.Dentists.Add(new Dentist
            {
                UserId = user.UserId,
                FullName = fullName,
                Specialization = specialization,
                ContactNumber = contactNumber
            });
            await _db.SaveChangesAsync();

            await _audit.LogAsync(GetUserId(), $"Added dentist '{fullName}'", "Dentists", user.UserId, GetIp());
            TempData["Success"] = "Dentist account created.";
            return RedirectToAction("Users");
        }

        // ── Audit Log ──────────────────────────────────────────
        public async Task<IActionResult> AuditLog()
        {
            var logs = await _db.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.PerformedAt)
                .Take(200)
                .ToListAsync();
            return View(logs);
        }
    }
}
