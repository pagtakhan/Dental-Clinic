using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalClinic.Models
{
    // ─── USER ───────────────────────────────────────────────
    public class User
    {
        [Key] public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = "";

        [Required, StringLength(255)]
        public string PasswordHash { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Patient"; // Admin | Dentist | Patient

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Patient? Patient { get; set; }
        public Dentist? Dentist { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    // ─── PATIENT ─────────────────────────────────────────────
    public class Patient
    {
        [Key] public int PatientId { get; set; }
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        public DateTime DateOfBirth { get; set; }

        [StringLength(20)]
        public string ContactNumber { get; set; } = "";

        public string? Address { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Billing> Billings { get; set; } = new List<Billing>();
    }

    // ─── DENTIST ─────────────────────────────────────────────
    public class Dentist
    {
        [Key] public int DentistId { get; set; }
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [StringLength(100)]
        public string Specialization { get; set; } = "General Dentistry";

        [StringLength(20)]
        public string ContactNumber { get; set; } = "";

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    // ─── APPOINTMENT ─────────────────────────────────────────
    public class Appointment
    {
        [Key] public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DentistId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required, StringLength(200)]
        public string Purpose { get; set; } = "";

        // Scheduled | Completed | Cancelled
        public string Status { get; set; } = "Scheduled";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [ForeignKey("DentistId")]
        public Dentist? Dentist { get; set; }

        public Billing? Billing { get; set; }
    }

    // ─── BILLING ─────────────────────────────────────────────
    public class Billing
    {
        [Key] public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; } = 0;

        // Paid | Unpaid | Partial
        public string PaymentStatus { get; set; } = "Unpaid";

        public string? Services { get; set; } // comma-separated
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }
    }

    // ─── AUDIT LOG ────────────────────────────────────────────
    public class AuditLog
    {
        [Key] public int LogId { get; set; }
        public int UserId { get; set; }

        [Required, StringLength(200)]
        public string Action { get; set; } = "";

        [StringLength(50)]
        public string TableAffected { get; set; } = "";

        public int? RecordId { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }

    // ─── VIEW MODELS ─────────────────────────────────────────
    public class LoginViewModel
    {
        [Required] public string Username { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    }

    public class RegisterViewModel
    {
        [Required, StringLength(50)] public string Username { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password), MinLength(8)] public string Password { get; set; } = "";
        [Required, StringLength(100)] public string FullName { get; set; } = "";
        [Required] public DateTime DateOfBirth { get; set; }
        [StringLength(20)] public string ContactNumber { get; set; } = "";
        public string? Address { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
    }

    public class AppointmentViewModel
    {
        public int AppointmentId { get; set; }
        [Required] public int DentistId { get; set; }
        [Required] public DateTime AppointmentDate { get; set; }
        [Required, StringLength(200)] public string Purpose { get; set; } = "";
        public string? Notes { get; set; }
    }

    public class BillingViewModel
    {
        public int AppointmentId { get; set; }
        [Required] public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string Services { get; set; } = "";
    }

    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TodayAppointments { get; set; }
        public int PendingBills { get; set; }
        public int TotalDentists { get; set; }
        public List<Appointment> RecentAppointments { get; set; } = new();
    }
}
