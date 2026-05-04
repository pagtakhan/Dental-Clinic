using DentalClinic.Data;
using DentalClinic.Models;

namespace DentalClinic.Services
{
    public class AuditService
    {
        private readonly AppDbContext _db;

        public AuditService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(int userId, string action, string table, int? recordId = null, string? ip = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                TableAffected = table,
                RecordId = recordId,
                IpAddress = ip,
                PerformedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
