-- Insert 50 sample sales opportunities into tbl_Sales_Pipeline
-- Note: Ensure tbl_Customers, tbl_Users, and tbl_Pipeline_Stages tables have data first
-- Assumes stage_id 1-5 exist (e.g., Lead, Qualified, Proposal, Negotiation, Closed Won)

USE [DB_CRM_MarFin]
GO

SET IDENTITY_INSERT tbl_Sales_Pipeline ON;

INSERT INTO tbl_Sales_Pipeline (opportunity_id, customer_id, assigned_to, stage_id, opportunity_name, deal_value, probability, expected_close_date, actual_close_date, close_reason, notes, is_archived, created_date, modified_date)
VALUES
(132, 1, 1, 5, 'Annual Supply Contract 2023', 125000.00, 100, '2023-01-30', '2023-01-28', 'Won - Contract signed', 'Major annual contract', 0, '2023-01-05', '2023-01-28'),
(234, 2, 1, 5, 'Enterprise Package Deal', 85000.00, 100, '2023-02-28', '2023-02-25', 'Won - Full package purchased', NULL, 0, '2023-02-01', '2023-02-25'),
(353, 3, 2, 5, 'Manufacturing Equipment Order', 210000.00, 100, '2023-03-15', '2023-03-12', 'Won - Large order completed', NULL, 0, '2023-02-20', '2023-03-12'),
(4, 5, 2, 5, 'Maintenance Service Agreement', 95000.00, 100, '2023-05-01', '2023-04-28', 'Won - 12-month contract', NULL, 0, '2023-04-10', '2023-04-28'),
(5, 6, 1, 5, 'Premium Service Package', 145000.00, 100, '2023-05-30', '2023-05-28', 'Won - Premium tier selected', NULL, 0, '2023-05-05', '2023-05-28'),
(6, 8, 1, 5, 'Project Implementation Q2', 72000.00, 100, '2023-06-30', '2023-06-25', 'Won - Project started', NULL, 0, '2023-05-20', '2023-06-25'),
(7, 9, 2, 5, 'Annual Manufacturing Contract', 189000.00, 100, '2023-07-31', '2023-07-28', 'Won - Contract renewal', NULL, 0, '2023-07-01', '2023-07-28'),
(8, 11, 2, 5, 'Logistics Partnership Deal', 110000.00, 100, '2023-08-31', '2023-08-28', 'Won - Partnership established', NULL, 0, '2023-08-05', '2023-08-28'),
(9, 12, 1, 5, 'Consulting Retainer Agreement', 156000.00, 100, '2023-09-30', '2023-09-25', 'Won - Retainer signed', NULL, 0, '2023-09-01', '2023-09-25'),
(10, 14, 1, 5, 'Property Development Deal', 92000.00, 100, '2023-10-15', '2023-10-12', 'Won - Initial phase approved', NULL, 0, '2023-09-25', '2023-10-12'),
(11, 15, 2, 5, 'Food Manufacturing Supply', 178000.00, 100, '2023-11-30', '2023-11-28', 'Won - Long-term supply contract', NULL, 0, '2023-11-01', '2023-11-28'),
(12, 17, 2, 5, 'Construction Supplies Contract', 135000.00, 100, '2023-12-20', '2023-12-18', 'Won - Bulk order placed', NULL, 0, '2023-11-28', '2023-12-18'),
(13, 18, 1, 5, 'Medical Supplies Distribution', 198000.00, 100, '2024-01-31', '2024-01-28', 'Won - Distribution agreement', NULL, 0, '2024-01-05', '2024-01-28'),
(14, 20, 1, 5, 'Fashion Retail Partnership', 88000.00, 100, '2024-02-28', '2024-02-25', 'Won - Retail chain contract', NULL, 0, '2024-02-01', '2024-02-25'),
(15, 21, 2, 5, 'IT Services Package', 167000.00, 100, '2024-03-20', '2024-03-18', 'Won - Annual IT support', NULL, 0, '2024-02-28', '2024-03-18'),
(16, 23, 2, 5, 'Automotive Parts Supply', 103000.00, 100, '2024-04-15', '2024-04-12', 'Won - Supplier agreement', NULL, 0, '2024-03-25', '2024-04-12'),
(17, 24, 1, 5, 'Hotel Chain Procurement', 215000.00, 100, '2024-05-30', '2024-05-28', 'Won - Major hotel contract', NULL, 0, '2024-05-01', '2024-05-28'),
(18, 26, 1, 5, 'Electronics Retail Deal', 94000.00, 100, '2024-06-30', '2024-06-28', 'Won - Retail distribution', NULL, 0, '2024-06-10', '2024-06-28'),
(19, 27, 2, 5, 'Textile Manufacturing Order', 142000.00, 100, '2024-07-25', '2024-07-22', 'Won - Large textile order', NULL, 0, '2024-07-01', '2024-07-22'),
(20, 29, 2, 5, 'Packaging Solutions Contract', 76000.00, 100, '2024-08-20', '2024-08-18', 'Won - Packaging partnership', NULL, 0, '2024-07-28', '2024-08-18'),
(21, 30, 1, 5, 'Investment Portfolio Service', 195000.00, 100, '2024-09-15', '2024-09-12', 'Won - Portfolio management', NULL, 0, '2024-08-25', '2024-09-12'),
(22, 32, 1, 5, 'Catering Services Agreement', 67000.00, 100, '2024-10-20', '2024-10-18', 'Won - Event catering contract', NULL, 0, '2024-10-01', '2024-10-18'),
(23, 33, 2, 5, 'Energy Solutions Deal', 187000.00, 100, '2024-11-15', '2024-11-12', 'Won - Energy services contract', NULL, 0, '2024-10-25', '2024-11-12'),
(24, 35, 2, 5, 'Security Services Contract', 98000.00, 100, '2024-12-10', '2024-12-08', 'Won - Annual security contract', NULL, 0, '2024-11-20', '2024-12-08'),
(25, 36, 1, 5, 'Venture Capital Partnership', 223000.00, 100, '2025-01-20', '2025-01-18', 'Won - VC partnership deal', NULL, 0, '2025-01-01', '2025-01-18'),
(26, 38, 1, 5, 'Marketing Services Package', 81000.00, 100, '2025-02-25', '2025-02-22', 'Won - Marketing contract', NULL, 0, '2025-02-05', '2025-02-22'),
(27, 39, 2, 5, 'Pharmaceutical Distribution', 162000.00, 100, '2025-03-30', '2025-03-28', 'Won - Pharma distribution deal', NULL, 0, '2025-03-10', '2025-03-28'),
(28, 41, 2, 5, 'Shipping & Logistics Deal', 119000.00, 100, '2025-04-20', '2025-04-18', 'Won - Logistics partnership', NULL, 0, '2025-04-01', '2025-04-18'),
(29, 42, 1, 5, 'Real Estate Development', 234000.00, 100, '2025-05-25', '2025-05-22', 'Won - Development project', NULL, 0, '2025-05-05', '2025-05-22'),
(30, 44, 1, 5, 'Furniture Retail Contract', 73000.00, 100, '2025-06-30', '2025-06-28', 'Won - Retail supply agreement', NULL, 0, '2025-06-15', '2025-06-28'),
(31, 45, 2, 5, 'Import/Export Partnership', 158000.00, 100, '2025-07-20', '2025-07-18', 'Won - Trade partnership', NULL, 0, '2025-07-01', '2025-07-18'),
(32, 47, 2, 5, 'Business Services Package', 86000.00, 100, '2025-08-15', '2025-08-12', 'Won - Service agreement', NULL, 0, '2025-07-28', '2025-08-12'),
(33, 48, 1, 5, 'Wellness Chain Expansion', 176000.00, 100, '2025-09-20', '2025-09-18', 'Won - Chain expansion deal', NULL, 0, '2025-09-01', '2025-09-18'),
(34, 50, 1, 5, 'Printing Services Contract', 91000.00, 100, '2025-10-15', '2025-10-12', 'Won - Commercial printing', NULL, 0, '2025-10-01', '2025-10-12'),
(35, 1, 1, 4, 'Q4 Expansion Deal', 85000.00, 75, '2025-12-20', NULL, NULL, 'In final negotiation stage', 0, '2025-10-15', '2025-11-20'),
(36, 3, 2, 4, 'New Equipment Upgrade', 125000.00, 80, '2025-12-31', NULL, NULL, 'Contract review in progress', 0, '2025-10-20', '2025-11-22'),
(37, 6, 1, 3, 'Additional Services Package', 65000.00, 60, '2026-01-15', NULL, NULL, 'Proposal submitted', 0, '2025-11-01', '2025-11-18'),
(38, 9, 2, 3, 'Manufacturing Line Extension', 95000.00, 65, '2026-01-31', NULL, NULL, 'Proposal under review', 0, '2025-11-05', '2025-11-20'),
(39, 12, 1, 4, 'Consulting Expansion Project', 78000.00, 70, '2026-02-15', NULL, NULL, 'Final pricing negotiation', 0, '2025-11-08', '2025-11-22'),
(40, 15, 2, 3, 'Food Supply Chain Upgrade', 110000.00, 55, '2026-02-28', NULL, NULL, 'Technical proposal sent', 0, '2025-11-10', '2025-11-19'),
(41, 18, 1, 2, 'Medical Equipment Purchase', 145000.00, 40, '2026-03-15', NULL, NULL, 'Qualified lead, needs assessment', 0, '2025-11-12', '2025-11-15'),
(42, 21, 2, 3, 'IT Infrastructure Upgrade', 88000.00, 50, '2026-03-31', NULL, NULL, 'Proposal preparation', 0, '2025-11-14', '2025-11-20'),
(43, 24, 1, 4, 'Hotel Chain Technology', 132000.00, 75, '2026-04-15', NULL, NULL, 'Contract terms discussion', 0, '2025-11-16', '2025-11-23'),
(44, 27, 2, 2, 'Textile Automation Project', 98000.00, 35, '2026-04-30', NULL, NULL, 'Initial qualification', 0, '2025-11-18', '2025-11-18'),
(45, 30, 1, 3, 'Investment Advisory Services', 115000.00, 60, '2026-05-15', NULL, NULL, 'Proposal submitted', 0, '2025-11-20', '2025-11-22'),
(46, 4, 1, 1, 'Retail Solutions Package', 45000.00, 20, '2026-06-30', NULL, NULL, 'Initial contact made', 0, '2025-11-15', '2025-11-15'),
(47, 7, 2, 1, 'Product Demo Request', 35000.00, 15, '2026-07-15', NULL, NULL, 'Demo scheduled', 0, '2025-11-18', '2025-11-18'),
(48, 10, 1, 1, 'Catalog Download Follow-up', 28000.00, 10, '2026-07-31', NULL, NULL, 'Lead qualification needed', 0, '2025-11-20', '2025-11-20'),
(49, 16, 1, 2, 'Demo Follow-up Opportunity', 52000.00, 30, '2026-08-15', NULL, NULL, 'Post-demo assessment', 0, '2025-11-22', '2025-11-22'),
(50, 22, 2, 1, 'Newsletter Subscriber Lead', 38000.00, 15, '2026-08-31', NULL, NULL, 'Initial outreach planned', 0, '2025-11-24', '2025-11-24');

SET IDENTITY_INSERT tbl_Sales_Pipeline OFF;

-- Display confirmation
SELECT COUNT(*) AS TotalOpportunitiesInserted FROM tbl_Sales_Pipeline;
SELECT 
    ps.stage_name,
    COUNT(*) AS Count,
    SUM(sp.deal_value) AS TotalValue
FROM tbl_Sales_Pipeline sp
INNER JOIN tbl_Pipeline_Stages ps ON sp.stage_id = ps.stage_id
GROUP BY ps.stage_name, ps.stage_order
ORDER BY ps.stage_order;
SELECT TOP 10 opportunity_id, opportunity_name, deal_value, probability, expected_close_date 
FROM tbl_Sales_Pipeline 
ORDER BY opportunity_id DESC;