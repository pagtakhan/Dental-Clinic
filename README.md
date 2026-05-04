# 🦷 Dental Clinic Appointment System
### ASP.NET Core 8 + MySQL | ITP 321 Final Project

---

## 📋 Features

| Module | Admin | Dentist | Patient |
|---|---|---|---|
| Login / Register | ✅ | ✅ | ✅ |
| Dashboard | ✅ Full Stats | ✅ Schedule | ✅ Upcoming |
| Appointments | ✅ All | ✅ Assigned | ✅ Own |
| Patient Records | ✅ Full CRUD | ✅ Read (assigned) | ❌ |
| Billing | ✅ Full | ❌ | ✅ View own |
| User Management | ✅ | ❌ | ❌ |
| Audit Log | ✅ | ❌ | ❌ |

---

## 🔒 Security Features
- **BCrypt** password hashing (work factor 11)
- **Account lockout** after 5 failed login attempts (15 min)
- **RBAC** – Role-Based Access Control enforced in every controller
- **Anti-forgery tokens** on all POST forms
- **Parameterized queries** via Entity Framework Core (prevents SQL injection)
- **Audit logging** – every critical action is recorded with timestamp + IP
- **Session expiry** – 30 minutes of inactivity
- **Secure error handling** – no stack traces shown to users

---

## ⚙️ Setup Instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MySQL 8.0+](https://dev.mysql.com/downloads/)
- Visual Studio 2022 or VS Code

### Step 1 – Database
```sql
-- Run in MySQL Workbench or terminal:
mysql -u root -p < dental_clinic_setup.sql
```

### Step 2 – Configure Connection String
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=dental_clinic_db;User=dental_app;Password=YourPassword123;"
}
```

### Step 3 – Run EF Migrations
```bash
cd DentalClinic
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Step 4 – Run the App
```bash
dotnet run
```
Then open: **https://localhost:5001**

---

## 🔑 Default Login
| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `Admin@1234` |

> ⚠️ Change the admin password immediately after first login!

---

## 📁 Project Structure
```
DentalClinic/
├── Controllers/
│   ├── AccountController.cs   ← Login, Register, Logout
│   ├── AdminController.cs     ← Dashboard, Users, Audit Log
│   ├── AppointmentsController.cs
│   ├── PatientsController.cs
│   ├── BillingController.cs
│   └── HomeController.cs
├── Models/
│   └── Models.cs              ← All entities + ViewModels
├── Data/
│   └── AppDbContext.cs        ← EF Core DbContext + seed data
├── Services/
│   └── AuditService.cs        ← Security audit logging
├── Views/
│   ├── Shared/_Layout.cshtml  ← Master layout
│   ├── Account/               ← Login, Register
│   ├── Admin/                 ← Dashboard, Users, AuditLog
│   ├── Appointments/          ← Index, Book, Details
│   ├── Patients/              ← Index, Details, Edit
│   └── Billing/               ← Index, Generate, Details
├── wwwroot/
│   ├── css/site.css           ← Custom dental theme
│   └── js/site.js
├── Program.cs                 ← App startup + DI config
├── appsettings.json           ← DB connection string
└── dental_clinic_setup.sql    ← MySQL setup script
```

---

## 🛠️ Tech Stack
- **Backend:** ASP.NET Core 8 MVC (C#)
- **ORM:** Entity Framework Core 8
- **Database:** MySQL 8.0
- **Auth:** Cookie Authentication + BCrypt.Net
- **Frontend:** Razor Views + Custom CSS (no external frameworks)
