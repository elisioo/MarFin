-- Insert 50 sample campaigns into tbl_Campaigns
-- Note: Ensure tbl_Users table has data first
-- This script assumes user_id 1-5 exist

USE [DB_CRM_MarFin]
GO

SET IDENTITY_INSERT tbl_Campaigns ON;

INSERT INTO tbl_Campaigns (campaign_id, created_by, campaign_name, campaign_type, start_date, end_date, budget, actual_spend, subject_line, email_content, campaign_status, total_sent, total_opened, total_clicked, total_converted, revenue_generated, is_archived, created_date, modified_date)
VALUES
-- 2023 Campaigns
(1, 1, 'New Year Promotion 2023', 'Email', '2023-01-01', '2023-01-31', 50000.00, 48500.00, 'Start 2023 with Amazing Deals!', 'Exclusive New Year offers just for you...', 'Completed', 5000, 2250, 675, 85, 425000.00, 0, '2022-12-20', '2023-02-01'),
(2, 2, 'Valentine Marketing Push', 'Email', '2023-02-01', '2023-02-14', 30000.00, 28900.00, 'Show Your Love This Valentine', 'Special Valentine packages available...', 'Completed', 3500, 1575, 472, 62, 310000.00, 0, '2023-01-25', '2023-02-15'),
(3, 1, 'Spring Product Launch', 'Email', '2023-03-15', '2023-04-15', 75000.00, 72300.00, 'Introducing Our Spring Collection', 'New arrivals for the spring season...', 'Completed', 8000, 3600, 1080, 142, 710000.00, 0, '2023-03-01', '2023-04-16'),
(4, 3, 'Q1 Customer Appreciation', 'SMS', '2023-03-20', '2023-03-31', 15000.00, 14200.00, 'Thank You for Being Valued Customer', 'Special discount for loyal customers...', 'Completed', 2000, 1600, 480, 45, 225000.00, 0, '2023-03-10', '2023-04-01'),
(5, 2, 'Easter Special Campaign', 'Email', '2023-04-01', '2023-04-16', 35000.00, 33800.00, 'Celebrate Easter with Us', 'Easter bundles and special offers...', 'Completed', 4200, 1890, 567, 73, 365000.00, 0, '2023-03-25', '2023-04-17'),

-- Q2 2023 Campaigns
(6, 1, 'Summer Sale Kickoff', 'Email', '2023-05-01', '2023-05-31', 80000.00, 78500.00, 'Summer Savings Are Here!', 'Beat the heat with cool deals...', 'Completed', 9000, 4050, 1215, 156, 780000.00, 0, '2023-04-20', '2023-06-01'),
(7, 2, 'Mother Day Special', 'Email', '2023-05-08', '2023-05-14', 25000.00, 24100.00, 'Perfect Gifts for Mom', 'Show mom how much you care...', 'Completed', 3000, 1350, 405, 58, 290000.00, 0, '2023-05-01', '2023-05-15'),
(8, 3, 'Mid-Year Flash Sale', 'Social', '2023-06-15', '2023-06-20', 40000.00, 38900.00, '48-Hour Flash Sale Alert', 'Limited time offers you cannot miss...', 'Completed', 5500, 2475, 742, 89, 445000.00, 0, '2023-06-10', '2023-06-21'),
(9, 1, 'Father Day Campaign', 'Email', '2023-06-10', '2023-06-18', 28000.00, 27300.00, 'Great Gifts for Dad', 'Celebrate dad with special offers...', 'Completed', 3200, 1440, 432, 54, 270000.00, 0, '2023-06-05', '2023-06-19'),
(10, 2, 'Summer Clearance Event', 'Email', '2023-06-25', '2023-07-15', 60000.00, 58400.00, 'Massive Summer Clearance', 'Up to 70% off selected items...', 'Completed', 7000, 3150, 945, 121, 605000.00, 0, '2023-06-20', '2023-07-16'),

-- Q3 2023 Campaigns
(11, 1, 'Back to School Promo', 'Email', '2023-07-15', '2023-08-15', 70000.00, 68200.00, 'Get Ready for School Year', 'Everything you need for school...', 'Completed', 8500, 3825, 1147, 143, 715000.00, 0, '2023-07-10', '2023-08-16'),
(12, 3, 'Independence Day Sale', 'Email', '2023-08-10', '2023-08-31', 45000.00, 43800.00, 'Celebrate Independence with Savings', 'Patriotic deals and offers...', 'Completed', 5200, 2340, 702, 88, 440000.00, 0, '2023-08-05', '2023-09-01'),
(13, 2, 'Labor Day Weekend Special', 'Social', '2023-09-01', '2023-09-04', 20000.00, 19400.00, 'Labor Day Deals', 'Long weekend special offers...', 'Completed', 2800, 1260, 378, 42, 210000.00, 0, '2023-08-28', '2023-09-05'),
(14, 1, 'Fall Collection Launch', 'Email', '2023-09-15', '2023-10-15', 85000.00, 82700.00, 'Fall Fashion is Here', 'Discover the latest fall trends...', 'Completed', 9500, 4275, 1282, 165, 825000.00, 0, '2023-09-10', '2023-10-16'),
(15, 2, 'Mid-Autumn Festival', 'Email', '2023-09-25', '2023-09-29', 32000.00, 31200.00, 'Celebrate Mid-Autumn with Us', 'Special festival packages...', 'Completed', 3800, 1710, 513, 64, 320000.00, 0, '2023-09-20', '2023-09-30'),

-- Q4 2023 Campaigns
(16, 1, 'Halloween Spooktacular', 'Email', '2023-10-15', '2023-10-31', 38000.00, 37100.00, 'Spooky Savings This Halloween', 'Frighteningly good deals...', 'Completed', 4500, 2025, 607, 76, 380000.00, 0, '2023-10-10', '2023-11-01'),
(17, 3, 'November Mega Sale', 'Email', '2023-11-01', '2023-11-30', 95000.00, 92800.00, 'November to Remember', 'Month-long spectacular deals...', 'Completed', 10000, 4500, 1350, 178, 890000.00, 0, '2023-10-25', '2023-12-01'),
(18, 1, 'Black Friday Blowout', 'Email', '2023-11-24', '2023-11-24', 120000.00, 118500.00, 'Black Friday - Biggest Sale of Year', 'Unbeatable Black Friday prices...', 'Completed', 15000, 6750, 2025, 285, 1425000.00, 0, '2023-11-15', '2023-11-25'),
(19, 2, 'Cyber Monday Deals', 'Email', '2023-11-27', '2023-11-27', 110000.00, 108200.00, 'Cyber Monday - Online Only', 'Exclusive online mega deals...', 'Completed', 14000, 6300, 1890, 256, 1280000.00, 0, '2023-11-20', '2023-11-28'),
(20, 1, 'Holiday Gift Guide', 'Email', '2023-12-01', '2023-12-24', 100000.00, 98400.00, 'Perfect Holiday Gifts', 'Find the perfect gift for everyone...', 'Completed', 12000, 5400, 1620, 215, 1075000.00, 0, '2023-11-28', '2023-12-25'),

-- 2024 Campaigns
(21, 2, 'Christmas Countdown', 'Email', '2023-12-15', '2023-12-25', 85000.00, 83600.00, 'Days Until Christmas Sale', 'Daily deals leading to Christmas...', 'Completed', 11000, 4950, 1485, 198, 990000.00, 0, '2023-12-10', '2023-12-26'),
(22, 1, 'New Year Resolution 2024', 'Email', '2024-01-01', '2024-01-31', 55000.00, 53800.00, 'Achieve Your 2024 Goals', 'Start the year right with us...', 'Completed', 6000, 2700, 810, 102, 510000.00, 0, '2023-12-28', '2024-02-01'),
(23, 3, 'Valentine Romance Package', 'Email', '2024-02-01', '2024-02-14', 42000.00, 40900.00, 'Romance is in the Air', 'Make this Valentine unforgettable...', 'Completed', 4800, 2160, 648, 82, 410000.00, 0, '2024-01-25', '2024-02-15'),
(24, 2, 'Leap Year Special', 'SMS', '2024-02-29', '2024-02-29', 18000.00, 17500.00, 'Leap into Savings', 'Once every 4 years deal...', 'Completed', 2500, 2000, 600, 52, 260000.00, 0, '2024-02-25', '2024-03-01'),
(25, 1, 'Spring Forward Sale', 'Email', '2024-03-15', '2024-04-15', 78000.00, 76200.00, 'Spring into Savings', 'Fresh deals for spring season...', 'Completed', 8800, 3960, 1188, 148, 740000.00, 0, '2024-03-10', '2024-04-16'),

-- Q2 2024 Campaigns
(26, 2, 'Easter Egg Hunt Promo', 'Email', '2024-03-28', '2024-03-31', 33000.00, 32100.00, 'Find Amazing Easter Deals', 'Hunt for savings this Easter...', 'Completed', 3900, 1755, 526, 68, 340000.00, 0, '2024-03-25', '2024-04-01'),
(27, 1, 'Earth Day Campaign', 'Social', '2024-04-22', '2024-04-22', 25000.00, 24300.00, 'Go Green with Our Eco Products', 'Sustainable choices for Earth Day...', 'Completed', 3100, 1395, 418, 48, 240000.00, 0, '2024-04-18', '2024-04-23'),
(28, 3, 'May Madness Sale', 'Email', '2024-05-01', '2024-05-31', 82000.00, 80100.00, 'May Brings Amazing Deals', 'Month-long savings event...', 'Completed', 9200, 4140, 1242, 159, 795000.00, 0, '2024-04-25', '2024-06-01'),
(29, 2, 'Mother Day Tribute', 'Email', '2024-05-10', '2024-05-12', 36000.00, 35200.00, 'Honor Mom with Perfect Gifts', 'Special gifts for special moms...', 'Completed', 4100, 1845, 553, 71, 355000.00, 0, '2024-05-05', '2024-05-13'),
(30, 1, 'Summer Kickoff 2024', 'Email', '2024-06-01', '2024-06-30', 88000.00, 86400.00, 'Summer Starts with Savings', 'Hot summer deals are here...', 'Completed', 9800, 4410, 1323, 172, 860000.00, 0, '2024-05-28', '2024-07-01'),

-- Q3 2024 Campaigns
(31, 2, 'Father Day Celebration', 'Email', '2024-06-14', '2024-06-16', 34000.00, 33100.00, 'Dad Deserves the Best', 'Special Father Day offers...', 'Completed', 3700, 1665, 499, 63, 315000.00, 0, '2024-06-10', '2024-06-17'),
(32, 1, 'Independence Sale 2024', 'Email', '2024-08-15', '2024-08-31', 52000.00, 50800.00, 'Celebrate Freedom with Deals', 'Independence Day special pricing...', 'Completed', 5800, 2610, 783, 97, 485000.00, 0, '2024-08-10', '2024-09-01'),
(33, 3, 'Back to School 2024', 'Email', '2024-07-20', '2024-08-20', 72000.00, 70400.00, 'School Essentials at Great Prices', 'Everything for the new school year...', 'Completed', 8200, 3690, 1107, 138, 690000.00, 0, '2024-07-15', '2024-08-21'),
(34, 2, 'Labor Day Extravaganza', 'Social', '2024-09-02', '2024-09-02', 28000.00, 27200.00, 'Work Less, Save More', 'Labor Day exclusive deals...', 'Completed', 3300, 1485, 445, 54, 270000.00, 0, '2024-08-30', '2024-09-03'),
(35, 1, 'Fall Fashion Week', 'Email', '2024-09-20', '2024-10-20', 90000.00, 88200.00, 'Fall in Love with Fashion', 'Latest fall fashion trends...', 'Completed', 10200, 4590, 1377, 182, 910000.00, 0, '2024-09-15', '2024-10-21'),

-- Q4 2024 Campaigns
(36, 2, 'October Fest Deals', 'Email', '2024-10-01', '2024-10-31', 65000.00, 63500.00, 'Celebrate October with Savings', 'Autumn deals all month long...', 'Completed', 7200, 3240, 972, 124, 620000.00, 0, '2024-09-28', '2024-11-01'),
(37, 1, 'Halloween Horror Sale', 'Email', '2024-10-25', '2024-10-31', 42000.00, 40800.00, 'Scary Good Deals', 'Terrifyingly good Halloween prices...', 'Completed', 4600, 2070, 621, 78, 390000.00, 0, '2024-10-20', '2024-11-01'),
(38, 3, 'November Nonstop Deals', 'Email', '2024-11-01', '2024-11-30', 98000.00, 96200.00, 'November Never Stops', 'Continuous deals all November...', 'Completed', 10500, 4725, 1417, 186, 930000.00, 0, '2024-10-28', '2024-12-01'),
(39, 1, 'Black Friday 2024', 'Email', '2024-11-29', '2024-11-29', 150000.00, 148200.00, 'Black Friday Mega Blowout', 'Biggest sale event of 2024...', 'Completed', 18000, 8100, 2430, 342, 1710000.00, 0, '2024-11-20', '2024-11-30'),
(40, 2, 'Cyber Monday Spectacular', 'Email', '2024-12-02', '2024-12-02', 140000.00, 138500.00, 'Cyber Monday - Digital Deals', 'Online exclusive mega savings...', 'Completed', 16500, 7425, 2227, 312, 1560000.00, 0, '2024-11-25', '2024-12-03'),

-- 2025 Current/Active Campaigns
(41, 1, 'Holiday Season 2024', 'Email', '2024-12-05', '2024-12-25', 115000.00, 95000.00, 'Holiday Cheer and Savings', 'Festive offers for the holidays...', 'Active', 13500, 6075, 1822, 0, 0.00, 0, '2024-12-01', '2025-11-20'),
(42, 2, 'Christmas Gift Guide 2024', 'Email', '2024-12-10', '2024-12-24', 95000.00, 78000.00, 'Find Perfect Christmas Gifts', 'Gift ideas for everyone...', 'Active', 11500, 5175, 1552, 0, 0.00, 0, '2024-12-05', '2025-11-20'),
(43, 3, 'Year End Clearance', 'Email', '2024-12-26', '2024-12-31', 75000.00, 48000.00, 'End of Year Blowout', 'Clear inventory sale...', 'Active', 8500, 3825, 1147, 0, 0.00, 0, '2024-12-20', '2025-11-20'),
(44, 1, 'New Year New You 2025', 'Email', '2025-01-01', '2025-01-31', 60000.00, 42000.00, 'Transform in 2025', 'Start fresh with new products...', 'Active', 7000, 3150, 945, 0, 0.00, 0, '2024-12-28', '2025-11-20'),
(45, 2, 'January Sale Event', 'Email', '2025-01-15', '2025-02-15', 70000.00, 38000.00, 'January Savings Spectacular', 'New year, new deals...', 'Active', 8000, 3600, 1080, 0, 0.00, 0, '2025-01-10', '2025-11-20'),

-- Upcoming Scheduled Campaigns
(46, 1, 'Valentine Romance 2025', 'Email', '2025-02-01', '2025-02-14', 45000.00, 0.00, 'Love is in the Air', 'Romantic gifts and offers...', 'Scheduled', 0, 0, 0, 0, 0.00, 0, '2025-01-20', '2025-11-20'),
(47, 3, 'Spring Collection Preview', 'Email', '2025-03-01', '2025-03-31', 80000.00, 0.00, 'Preview Spring Arrivals', 'Get first look at spring collection...', 'Scheduled', 0, 0, 0, 0, 0.00, 0, '2025-02-15', '2025-11-20'),
(48, 2, 'Easter 2025 Campaign', 'Email', '2025-04-10', '2025-04-20', 40000.00, 0.00, 'Easter Bunny Savings', 'Hop into Easter deals...', 'Scheduled', 0, 0, 0, 0, 0.00, 0, '2025-03-25', '2025-11-20'),
(49, 1, 'Summer Preview Sale', 'Social', '2025-05-01', '2025-05-31', 85000.00, 0.00, 'Summer is Coming', 'Early summer collection launch...', 'Scheduled', 0, 0, 0, 0, 0.00, 0, '2025-04-15', '2025-11-20'),
(50, 2, 'Mid-Year Mega Event', 'Email', '2025-06-15', '2025-07-15', 100000.00, 0.00, 'Half Year, Full Savings', 'Mid-year clearance event...', 'Scheduled', 0, 0, 0, 0, 0.00, 0, '2025-06-01', '2025-11-20');

SET IDENTITY_INSERT tbl_Campaigns OFF;

-- Display confirmation and statistics
SELECT COUNT(*) AS TotalCampaignsInserted FROM tbl_Campaigns;
SELECT campaign_status, COUNT(*) AS Count FROM tbl_Campaigns GROUP BY campaign_status;
SELECT campaign_type, COUNT(*) AS Count FROM tbl_Campaigns GROUP BY campaign_type;

-- Show revenue statistics for completed campaigns
SELECT 
    COUNT(*) AS CompletedCampaigns,
    SUM(budget) AS TotalBudget,
    SUM(actual_spend) AS TotalSpent,
    SUM(revenue_generated) AS TotalRevenue,
    SUM(revenue_generated) - SUM(actual_spend) AS NetProfit,
    CAST(ROUND((SUM(revenue_generated) - SUM(actual_spend)) / SUM(actual_spend) * 100, 2) AS DECIMAL(10,2)) AS ROI_Percentage
FROM tbl_Campaigns
WHERE campaign_status = 'Completed';