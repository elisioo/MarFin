INSERT INTO tbl_Customer_Segments
    (segment_id, segment_name, description, min_revenue, max_revenue, is_active, created_date)
VALUES
    (1, 'Basic', 'Basic tier customers', 0.00, 50000.00, 1, '2025-11-16 22:31:12.6800000'),
    (2, 'Standard', 'Standard tier customers', 50000.00, 200000.00, 1, '2025-11-16 22:31:12.6800000'),
    (3, 'Premium', 'Premium tier customers', 200000.00, NULL, 1, '2025-11-16 22:31:12.6800000');
