-- Run this script to initialize roles in your database
-- This should be run once to set up the initial roles

USE [DB_CRM_MarFin]
GO

-- Check if roles already exist, if not insert them
IF NOT EXISTS (SELECT 1 FROM tbl_Roles WHERE role_name = 'Admin')
BEGIN
    INSERT INTO tbl_Roles (role_name, description, permissions, is_active, created_date, modified_date)
    VALUES ('Admin', 'Full system access with all permissions', 'ALL', 1, GETDATE(), GETDATE())
END

IF NOT EXISTS (SELECT 1 FROM tbl_Roles WHERE role_name = 'Finance')
BEGIN
    INSERT INTO tbl_Roles (role_name, description, permissions, is_active, created_date, modified_date)
    VALUES ('Finance', 'Access to financial records, invoices, and transactions', 'FINANCE,INVOICES,TRANSACTIONS,REPORTS', 1, GETDATE(), GETDATE())
END

IF NOT EXISTS (SELECT 1 FROM tbl_Roles WHERE role_name = 'Marketing')
BEGIN
    INSERT INTO tbl_Roles (role_name, description, permissions, is_active, created_date, modified_date)
    VALUES ('Marketing', 'Access to campaigns, customer segments, and marketing analytics', 'CAMPAIGNS,CUSTOMERS,SEGMENTS,REPORTS', 1, GETDATE(), GETDATE())
END

IF NOT EXISTS (SELECT 1 FROM tbl_Roles WHERE role_name = 'Sales Representative')
BEGIN
    INSERT INTO tbl_Roles (role_name, description, permissions, is_active, created_date, modified_date)
    VALUES ('Sales Representative', 'Access to sales pipeline, customer interactions, and opportunities', 'SALES,CUSTOMERS,INTERACTIONS,OPPORTUNITIES', 1, GETDATE(), GETDATE())
END

-- Display the roles
SELECT role_id, role_name, description, is_active 
FROM tbl_Roles 
ORDER BY role_id

GO