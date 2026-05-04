-- ============================================================
-- Dental Clinic Appointment System - MySQL Setup Script
-- Run this ONCE before starting the application
-- ============================================================

CREATE DATABASE IF NOT EXISTS dental_clinic_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE dental_clinic_db;

-- (EF Core Migrations will auto-create all tables on first run)
-- This script just ensures the DB exists.

-- Create a dedicated MySQL user for the app (recommended)
-- Replace 'YourPassword123' with a strong password
CREATE USER IF NOT EXISTS 'dental_app'@'localhost' IDENTIFIED BY 'YourPassword123';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, DROP
    ON dental_clinic_db.*
    TO 'dental_app'@'localhost';
FLUSH PRIVILEGES;

SELECT 'Database setup complete. Update appsettings.json with your credentials.' AS Status;
