using DentalClinic.Data;
using DentalClinic.Models;
using DentalClinic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DentalClinic.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly IHttpContextAccessor _http;

        public AppointmentsController(AppDbContext db, AuditService audit, IHttpContextAccessor http)
        {
            _db = db; _audit = audit; _http = http;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetRole() => User.FindFirstValue(ClaimTypes.Role)!;
        private string GetIp() => _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ── List ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var role = GetRole();
            var uid = GetUserId();

            IQueryable<Appointment> query = _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Dentist);

            if (role == "Patient")
            {
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
                if (patient == null) return RedirectToAction("Index", "Home");
                query = query.Where(a => a.PatientId == patient.PatientId);
            }
            else if (role == "Dentist")
            {
                var dentist = await _db.Dentists.FirstOrDefaultAsync(d => d.UserId == uid);
                if (dentist == null) return RedirectToAction("Index", "Home");
                query = query.Where(a => a.DentistId == dentist.DentistId);
            }

            return View(await query.OrderByDescending(a => a.AppointmentDate).ToListAsync());
        }

        // ── Book (Patient) ─────────────────────────────────────
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> Book()
        {
            ViewBag.Dentists = new SelectList(await _db.Dentists.ToListAsync(), "DentistId", "FullName");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> Book(AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Dentists = new SelectList(await _db.Dentists.ToListAsync(), "DentistId", "FullName");
                return View(model);
            }

            // Conflict check (parameterized via EF — prevents SQL injection)
            bool conflict = await _db.Appointments.AnyAsync(a =>
                a.DentistId == model.DentistId &&
                a.Status == "Scheduled" &&
                a.AppointmentDate >= model.AppointmentDate.AddMinutes(-30) &&
                a.AppointmentDate <= model.AppointmentDate.AddMinutes(30));

            if (conflict)
            {
                ModelState.AddModelError("AppointmentDate", "This dentist already has an appointment within 30 minutes of your selected time.");
                ViewBag.Dentists = new SelectList(await _db.Dentists.ToListAsync(), "DentistId", "FullName");
                return View(model);
            }

            var uid = GetUserId();
            int patientId;

            if (GetRole() == "Admin" && TempData.ContainsKey("AdminBookPatientId"))
                patientId = (int)TempData["AdminBookPatientId"]!;
            else
            {
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
                if (patient == null) return BadRequest();
                patientId = patient.PatientId;
            }

            var appt = new Appointment
            {
                PatientId = patientId,
                DentistId = model.DentistId,
                AppointmentDate = model.AppointmentDate,
                Purpose = model.Purpose,
                Notes = model.Notes,
                Status = "Scheduled"
            };
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(uid, $"Appointment booked for {model.AppointmentDate:yyyy-MM-dd HH:mm}", "Appointments", appt.AppointmentId, GetIp());
            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction("Index");
        }

        // ── Cancel ─────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            // Patients can only cancel their own
            if (GetRole() == "Patient")
            {
                var uid = GetUserId();
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
                if (patient == null || appt.PatientId != patient.PatientId)
                    return Forbid();
            }

            appt.Status = "Cancelled";
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetUserId(), "Appointment cancelled", "Appointments", id, GetIp());
            TempData["Success"] = "Appointment cancelled.";
            return RedirectToAction("Index");
        }

        // ── Complete (Admin/Dentist) ────────────────────────────
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Dentist")]
        public async Task<IActionResult> Complete(int id)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            appt.Status = "Completed";
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetUserId(), "Appointment marked completed", "Appointments", id, GetIp());
            TempData["Success"] = "Appointment marked as completed.";
            return RedirectToAction("Index");
        }

        // ── Details ────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var appt = await _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Dentist)
                .Include(a => a.Billing)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appt == null) return NotFound();

            // Patients can only see their own
            if (GetRole() == "Patient")
            {
                var uid = GetUserId();
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
                if (patient == null || appt.PatientId != patient.PatientId)
                    return Forbid();
            }

            return View(appt);
        }
    }
}
