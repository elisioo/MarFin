using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Ordering_Sorono_IT13.Models;

namespace Ordering_Sorono_IT13.Data
{
    public class CustomerSegmentService
    {
        // Get all active segments
        public List<CustomerSegment> GetAllSegments()
        {
            List<CustomerSegment> segments = new List<CustomerSegment>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT segment_id, segment_name, description, min_revenue, 
                                    max_revenue, is_active, created_date 
                                   FROM tbl_Customer_Segments 
                                   WHERE is_active = 1 
                                   ORDER BY segment_name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                segments.Add(new CustomerSegment
                                {
                                    SegmentId = Convert.ToInt32(reader["segment_id"]),
                                    SegmentName = reader["segment_name"].ToString(),
                                    Description = reader["description"].ToString(),
                                    MinRevenue = Convert.ToDecimal(reader["min_revenue"]),
                                    MaxRevenue = reader["max_revenue"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["max_revenue"]) : (decimal?)null,
                                    IsActive = Convert.ToBoolean(reader["is_active"]),
                                    CreatedDate = Convert.ToDateTime(reader["created_date"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting segments: " + ex.Message);
            }

            return segments;
        }

        // Get segment by ID
        public CustomerSegment GetSegmentById(int segmentId)
        {
            CustomerSegment segment = null;

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT segment_id, segment_name, description, min_revenue, 
                                    max_revenue, is_active, created_date 
                                   FROM tbl_Customer_Segments 
                                   WHERE segment_id = @SegmentId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SegmentId", segmentId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                segment = new CustomerSegment
                                {
                                    SegmentId = Convert.ToInt32(reader["segment_id"]),
                                    SegmentName = reader["segment_name"].ToString(),
                                    Description = reader["description"].ToString(),
                                    MinRevenue = Convert.ToDecimal(reader["min_revenue"]),
                                    MaxRevenue = reader["max_revenue"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["max_revenue"]) : (decimal?)null,
                                    IsActive = Convert.ToBoolean(reader["is_active"]),
                                    CreatedDate = Convert.ToDateTime(reader["created_date"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting segment: " + ex.Message);
            }

            return segment;
        }

        // Add default segments if none exist
        public void EnsureDefaultSegments()
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Check if segments exist
                    string checkQuery = "SELECT COUNT(*) FROM tbl_Customer_Segments";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            // Insert default segments
                            string insertQuery = @"
                                INSERT INTO tbl_Customer_Segments (segment_name, description, min_revenue, max_revenue, is_active, created_date)
                                VALUES 
                                ('Basic', 'Basic tier customers', 0, 50000, 1, GETDATE()),
                                ('Standard', 'Standard tier customers', 50000, 200000, 1, GETDATE()),
                                ('Premium', 'Premium tier customers', 200000, NULL, 1, GETDATE())";

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.ExecuteNonQuery();
                                Console.WriteLine("Default customer segments created successfully");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error ensuring default segments: " + ex.Message);
            }
        }
    }
}