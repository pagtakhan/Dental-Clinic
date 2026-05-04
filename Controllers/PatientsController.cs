using DentalClinic.Data;
using DentalClinic.Models;
using DentalClinic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DentalClinic.Controllers
{
    [Authorize(Roles = "Admin,Dentist")]
    public class PatientsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly IHttpContextAccessor _http;

        public PatientsController(AppDbContext db, AuditService audit, IHttpContextAccessor http)
        {
            _db = db; _audit = audit; _http = http;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetRole() => User.FindFirstValue(ClaimTypes.Role)!;
        private string GetIp() => _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ── List ───────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search)
        {
            IQueryable<Patient> query = _db.Patients.Include(p => p.User);

            // Dentist only sees patients they have appointments with
            if (GetRole() == "Dentist")
            {
                var uid = GetUserId();
                var dentist = await _db.Dentists.FirstOrDefaultAsync(d => d.UserId == uid);
                if (dentist == null) return RedirectToAction("Index", "Home");

                var patientIds = await _db.Appointments
                    .Where(a => a.DentistId == dentist.DentistId)
                    .Select(a => a.PatientId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(p => patientIds.Contains(p.PatientId));
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.FullName.Contains(search) || p.ContactNumber.Contains(search));

            ViewBag.Search = search;
            return View(await query.OrderBy(p => p.FullName).ToListAsync());
        }

        // ── Details ────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var patient = await _db.Patients
                .Include(p => p.User)
                .Include(p => p.Appointments).ThenInclude(a => a.Dentist)
                .Include(p => p.Billings)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null) return NotFound();

            // Dentist: verify the patient is theirs
            if (GetRole() == "Dentist")
            {
                var uid = GetUserId();
                var dentist = await _db.Dentists.FirstOrDefaultAsync(d => d.UserId == uid);
                bool isAssigned = dentist != null && await _db.Appointments
                    .AnyAsync(a => a.DentistId == dentist.DentistId && a.PatientId == id);
                if (!isAssigned) return Forbid();
            }

            return View(patient);
        }

        // ── Edit (Admin only) ──────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet] public async Task<IActionResult> Edit(int id)
        {
            var patient = await _db.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient model)
        {
            var patient = await _db.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            patient.FullName = model.FullName;
            patient.DateOfBirth = model.DateOfBirth;
            patient.ContactNumber = model.ContactNumber;
            patient.Address = model.Address;
            patient.Allergies = model.Allergies;
            patient.MedicalHistory = model.MedicalHistory;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(GetUserId(), $"Patient record updated for '{patient.FullName}'", "Patients", id, GetIp());
            TempData["Success"] = "Patient record updated.";
            return RedirectToAction("Details", new { id });
        }
    }
}
