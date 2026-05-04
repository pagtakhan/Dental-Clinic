using DentalClinic.Data;
using DentalClinic.Models;
using DentalClinic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DentalClinic.Controllers
{
    [Authorize]
    public class BillingController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly IHttpContextAccessor _http;

        public BillingController(AppDbContext db, AuditService audit, IHttpContextAccessor http)
        {
            _db = db; _audit = audit; _http = http;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetRole() => User.FindFirstValue(ClaimTypes.Role)!;
        private string GetIp() => _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ── List ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            IQueryable<Billing> query = _db.Billings
                .Include(b => b.Patient)
                .Include(b => b.Appointment).ThenInclude(a => a!.Dentist);

            if (GetRole() == "Patient")
            {
                var uid = GetUserId();
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
                if (patient == null) return RedirectToAction("Index", "Home");
                query = query.Where(b => b.PatientId == patient.PatientId);
            }

            return View(await query.OrderByDescending(b => b.IssuedAt).ToListAsync());
        }

        // ── Generate Bill (Admin) ──────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet] public async Task<IActionResult> Generate(int appointmentId)
        {
            var appt = await _db.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
            if (appt == null) return NotFound();

            if (appt.Status != "Completed")
            {
                TempData["Error"] = "Only completed appointments can be billed.";
                return RedirectToAction("Index", "Appointments");
            }

            if (await _db.Billings.AnyAsync(b => b.AppointmentId == appointmentId))
            {
                TempData["Error"] = "A bill already exists for this appointment.";
                return RedirectToAction("Index");
            }

            ViewBag.Appointment = appt;
            return View(new BillingViewModel { AppointmentId = appointmentId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(BillingViewModel model)
        {
            var appt = await _db.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);
            if (appt == null) return NotFound();

            var billing = new Billing
            {
                AppointmentId = model.AppointmentId,
                PatientId = appt.PatientId,
                TotalAmount = model.TotalAmount,
                AmountPaid = model.AmountPaid,
                Services = model.Services,
                PaymentStatus = model.AmountPaid >= model.TotalAmount ? "Paid"
                              : model.AmountPaid > 0 ? "Partial"
                              : "Unpaid"
            };
            _db.Billings.Add(billing);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(GetUserId(), $"Bill generated — ₱{model.TotalAmount}", "Billings", billing.BillId, GetIp());
            TempData["Success"] = "Bill generated successfully.";
            return RedirectToAction("Details", new { id = billing.BillId });
        }

        // ── Update Payment (Admin) ──────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePayment(int id, decimal amountPaid)
        {
            var bill = await _db.Billings.FindAsync(id);
            if (bill == null) return NotFound();

            bill.AmountPaid = amountPaid;
            bill.PaymentStatus = amountPaid >= bill.TotalAmount ? "Paid"
                               : amountPaid > 0 ? "Partial"
                               : "Unpaid";
            await _db.SaveChangesAsync();

            await _audit.LogAsync(GetUserId(), $"Payment updated — ₱{amountPaid} paid", "Billings", id, GetIp());
            TempData["Success"] = "Payment updated.";
            return RedirectToAction("Details", new { id });
        }

        // ── Details ────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var bill = await _db.Billings
                .Include(b => b.Patient)
                .Include(b => b.Appointment).ThenInclude(a => a!.Dentist)
                .FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null) return NotFound();

            // Patients can only see their own
            if (GetRole() == "Patient")
            {
                var uid = GetUserId();
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);
                if (patient == null || bill.PatientId != patient.PatientId)
                    return Forbid();
            }

            return View(bill);
        }
    }
}
