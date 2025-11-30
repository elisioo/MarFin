-- =============================================
-- Populate tbl_Invoice_Items
-- Sample data for invoice line items
-- =============================================

USE [DB_CRM_MarFin]
GO

-- Note: This assumes you have invoices with invoice_id values 1-10
-- Adjust the invoice_id values based on your actual invoice data


-- Invoice 13 - Content Writing Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(13, 1, 'Blog Post Writing (1000 words)', 15.00, 80.00, 1200.00),
(13, 2, 'Website Copy - Homepage', 1.00, 500.00, 500.00),
(13, 3, 'Product Description Writing', 25.00, 35.00, 875.00),
(13, 4, 'SEO Keyword Research', 1.00, 300.00, 300.00);

-- Invoice 14 - Event Planning Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(14, 1, 'Corporate Event Planning and Coordination', 1.00, 5000.00, 5000.00),
(14, 2, 'Venue Booking and Setup', 1.00, 3500.00, 3500.00),
(14, 3, 'Catering Services (per person)', 100.00, 45.00, 4500.00),
(14, 4, 'Audio/Visual Equipment Rental', 1.00, 1200.00, 1200.00);

-- Invoice 15 - Photography Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(15, 1, 'Corporate Photography Session (4 hours)', 4.00, 250.00, 1000.00),
(15, 2, 'Photo Editing and Retouching', 50.00, 15.00, 750.00),
(15, 3, 'High-Resolution Digital Files', 1.00, 500.00, 500.00);

-- Invoice 16 - Translation Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(16, 1, 'Document Translation (English to Spanish)', 5000.00, 0.12, 600.00),
(16, 2, 'Technical Manual Translation', 3000.00, 0.15, 450.00),
(16, 3, 'Proofreading and Quality Check', 8.00, 75.00, 600.00),
(16, 4, 'Certified Translation Service', 2.00, 100.00, 200.00);

-- Invoice 17 - Video Production Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(17, 1, 'Corporate Video Production', 1.00, 8000.00, 8000.00),
(17, 2, 'Script Writing and Storyboarding', 1.00, 1500.00, 1500.00),
(17, 3, 'Video Editing and Post-Production', 20.00, 150.00, 3000.00),
(17, 4, 'Background Music Licensing', 3.00, 200.00, 600.00);

-- Invoice 18 - HR Consulting Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(18, 1, 'Recruitment and Talent Acquisition', 40.00, 125.00, 5000.00),
(18, 2, 'Employee Onboarding Program Development', 1.00, 2000.00, 2000.00),
(18, 3, 'HR Policy Manual Creation', 1.00, 1500.00, 1500.00),
(18, 4, 'Performance Management System Setup', 1.00, 1800.00, 1800.00);

-- Invoice 19 - Insurance Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(19, 1, 'Business Liability Insurance - Annual', 1.00, 3500.00, 3500.00),
(19, 2, 'Property Insurance Coverage', 1.00, 2800.00, 2800.00),
(19, 3, 'Workers Compensation Insurance', 1.00, 2200.00, 2200.00);

-- Invoice 20 - Real Estate Services
INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
VALUES 
(20, 1, 'Property Management Services - Monthly', 6.00, 800.00, 4800.00),
(20, 2, 'Lease Agreement Preparation', 3.00, 150.00, 450.00),
(20, 3, 'Property Inspection Services', 3.00, 200.00, 600.00),
(20, 4, 'Tenant Screening Services', 5.00, 75.00, 375.00);

-- Query to verify the inserted data
SELECT 
    ii.item_id,
    ii.invoice_id,
    ii.item_order,
    ii.description,
    ii.quantity,
    ii.unit_price,
    ii.amount,
    CASE 
        WHEN ii.quantity * ii.unit_price = ii.amount THEN 'OK'
        ELSE 'MISMATCH'
    END AS calculation_check
FROM tbl_Invoice_Items ii
ORDER BY ii.invoice_id, ii.item_order;

-- Summary by invoice
SELECT 
    invoice_id,
    COUNT(*) AS item_count,
    SUM(amount) AS total_amount
FROM tbl_Invoice_Items
GROUP BY invoice_id
ORDER BY invoice_id;

PRINT 'Invoice items populated successfully!';