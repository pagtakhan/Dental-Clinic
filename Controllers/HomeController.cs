using DentalClinic.Data;
using DentalClinic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DentalClinic.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) { _db = db; }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Admin") return RedirectToAction("Index", "Admin");

            var uid = GetUserId();
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
            if (patient == null) return View("PatientSetupNeeded");

            var upcoming = await _db.Appointments
                .Include(a => a.Dentist)
                .Where(a => a.PatientId == patient.PatientId
                         && a.AppointmentDate >= DateTime.Now
                         && a.Status == "Scheduled")
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .ToListAsync();

            var unpaidBills = await _db.Billings
                .Where(b => b.PatientId == patient.PatientId && b.PaymentStatus != "Paid")
                .CountAsync();

            ViewBag.Patient = patient;
            ViewBag.UpcomingAppointments = upcoming;
            ViewBag.UnpaidBills = unpaidBills;
            return View();
        }

        public IActionResult Privacy() => View();
    }
}
