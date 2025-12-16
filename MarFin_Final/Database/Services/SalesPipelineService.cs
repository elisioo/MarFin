using MarFin_Final.Data;
using Microsoft.Data.SqlClient;
using MarFin_Final.Models;
using System;
using System.Collections.Generic;

namespace MarFin_Final.Data
{
    public class SalesPipelineService
    {
        // CREATE - Add new opportunity
        public bool AddOpportunity(SalesPipelineModel opportunity)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO tbl_Sales_Pipeline 
                                   (customer_id, assigned_to, stage_id, opportunity_name, 
                                    deal_value, probability, expected_close_date, notes, 
                                    is_archived, created_date, modified_date) 
                                   VALUES 
                                   (@CustomerId, @AssignedTo, @StageId, @OpportunityName, 
                                    @DealValue, @Probability, @ExpectedCloseDate, @Notes, 
                                    @IsArchived, @CreatedDate, @ModifiedDate)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", opportunity.CustomerId);
                        cmd.Parameters.AddWithValue("@AssignedTo", opportunity.AssignedTo);
                        cmd.Parameters.AddWithValue("@StageId", opportunity.StageId);
                        cmd.Parameters.AddWithValue("@OpportunityName", opportunity.OpportunityName);
                        cmd.Parameters.AddWithValue("@DealValue", opportunity.DealValue);
                        cmd.Parameters.AddWithValue("@Probability", opportunity.Probability);
                        cmd.Parameters.AddWithValue("@ExpectedCloseDate",
                            opportunity.ExpectedCloseDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", opportunity.Notes ?? "");
                        cmd.Parameters.AddWithValue("@IsArchived", false);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding opportunity: " + ex.Message);
                return false;
            }
        }

        //GetArchived
        public List<SalesPipelineModel> GetArchivedOpportunities()
        {
            List<SalesPipelineModel> opportunities = new List<SalesPipelineModel>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT 
                            sp.opportunity_id, sp.customer_id, sp.assigned_to, sp.stage_id,
                            sp.opportunity_name, sp.deal_value, sp.probability, 
                            sp.expected_close_date, sp.actual_close_date, sp.close_reason,
                            sp.notes, sp.is_archived, sp.archived_date, 
                            sp.created_date, sp.modified_date,
                            c.first_name + ' ' + c.last_name AS customer_name,
                            c.company_name,
                            ps.stage_name, ps.stage_order, ps.stage_color,
                            u.first_name + ' ' + u.last_name AS assigned_to_name
                           FROM tbl_Sales_Pipeline sp
                           INNER JOIN tbl_Customers c ON sp.customer_id = c.customer_id
                           INNER JOIN tbl_Pipeline_Stages ps ON sp.stage_id = ps.stage_id
                           INNER JOIN tbl_Users u ON sp.assigned_to = u.user_id
                           WHERE sp.is_archived = 1
                           ORDER BY sp.archived_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                opportunities.Add(MapOpportunityFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting archived opportunities: " + ex.Message);
            }

            return opportunities;
        }

        public List<SalesPipelineModel> GetAllOpportunities()
        {
            List<SalesPipelineModel> opportunities = new List<SalesPipelineModel>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT 
                            sp.opportunity_id, sp.customer_id, sp.assigned_to, sp.stage_id,
                            sp.opportunity_name, sp.deal_value, sp.probability, 
                            sp.expected_close_date, sp.actual_close_date, sp.close_reason,
                            sp.notes, sp.is_archived, sp.archived_date, 
                            sp.created_date, sp.modified_date,
                            c.first_name + ' ' + c.last_name AS customer_name,
                            c.company_name,
                            ps.stage_name, ps.stage_order, ps.stage_color,
                            u.first_name + ' ' + u.last_name AS assigned_to_name
                           FROM tbl_Sales_Pipeline sp
                           INNER JOIN tbl_Customers c ON sp.customer_id = c.customer_id
                           INNER JOIN tbl_Pipeline_Stages ps ON sp.stage_id = ps.stage_id
                           INNER JOIN tbl_Users u ON sp.assigned_to = u.user_id
                           WHERE sp.is_archived = 0
                           ORDER BY ps.stage_order, sp.created_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                opportunities.Add(MapOpportunityFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting opportunities: " + ex.Message);
            }

            return opportunities;
        }

        // READ - Get opportunity by ID
        public SalesPipelineModel GetOpportunityById(int opportunityId)
        {
            SalesPipelineModel? opportunity = null;

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT 
                                    sp.opportunity_id, sp.customer_id, sp.assigned_to, sp.stage_id,
                                    sp.opportunity_name, sp.deal_value, sp.probability, 
                                    sp.expected_close_date, sp.actual_close_date, sp.close_reason,
                                    sp.notes, sp.is_archived, sp.archived_date, 
                                    sp.created_date, sp.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.company_name,
                                    ps.stage_name, ps.stage_order, ps.stage_color,
                                    u.first_name + ' ' + u.last_name AS assigned_to_name
                                   FROM tbl_Sales_Pipeline sp
                                   INNER JOIN tbl_Customers c ON sp.customer_id = c.customer_id
                                   INNER JOIN tbl_Pipeline_Stages ps ON sp.stage_id = ps.stage_id
                                   INNER JOIN tbl_Users u ON sp.assigned_to = u.user_id
                                   WHERE sp.opportunity_id = @OpportunityId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OpportunityId", opportunityId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                opportunity = MapOpportunityFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting opportunity: " + ex.Message);
            }

            return opportunity ?? new SalesPipelineModel();
        }

        // UPDATE - Update existing opportunity
        public bool UpdateOpportunity(SalesPipelineModel opportunity)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Sales_Pipeline 
                                   SET customer_id = @CustomerId,
                                       assigned_to = @AssignedTo,
                                       stage_id = @StageId,
                                       opportunity_name = @OpportunityName,
                                       deal_value = @DealValue,
                                       probability = @Probability,
                                       expected_close_date = @ExpectedCloseDate,
                                       notes = @Notes,
                                       modified_date = @ModifiedDate
                                   WHERE opportunity_id = @OpportunityId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OpportunityId", opportunity.OpportunityId);
                        cmd.Parameters.AddWithValue("@CustomerId", opportunity.CustomerId);
                        cmd.Parameters.AddWithValue("@AssignedTo", opportunity.AssignedTo);
                        cmd.Parameters.AddWithValue("@StageId", opportunity.StageId);
                        cmd.Parameters.AddWithValue("@OpportunityName", opportunity.OpportunityName);
                        cmd.Parameters.AddWithValue("@DealValue", opportunity.DealValue);
                        cmd.Parameters.AddWithValue("@Probability", opportunity.Probability);
                        cmd.Parameters.AddWithValue("@ExpectedCloseDate",
                            opportunity.ExpectedCloseDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", opportunity.Notes ?? "");
                        cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating opportunity: " + ex.Message);
                return false;
            }
        }
        public bool RestoreOpportunity(int opportunityId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Sales_Pipeline 
                           SET is_archived = 0, 
                               archived_date = NULL,
                               modified_date = @ModifiedDate
                           WHERE opportunity_id = @OpportunityId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OpportunityId", opportunityId);
                        cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error restoring opportunity: " + ex.Message);
                return false;
            }
        }


        // ARCHIVE - Archive opportunity
        public bool ArchiveOpportunity(int opportunityId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Sales_Pipeline 
                                   SET is_archived = 1, 
                                       archived_date = @ArchivedDate
                                   WHERE opportunity_id = @OpportunityId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OpportunityId", opportunityId);
                        cmd.Parameters.AddWithValue("@ArchivedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error archiving opportunity: " + ex.Message);
                return false;
            }
        }

        // Get all pipeline stages
        public List<PipelineStageModel> GetAllStages()
        {
            List<PipelineStageModel> stages = new List<PipelineStageModel>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT stage_id, stage_name, stage_order, default_probability,
                                    description, is_closed, stage_color, is_active, created_date
                                   FROM tbl_Pipeline_Stages
                                   WHERE is_active = 1
                                   ORDER BY stage_order";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                stages.Add(new PipelineStageModel
                                {
                                    StageId = Convert.ToInt32(reader["stage_id"]),
                                    StageName = reader["stage_name"]?.ToString() ?? "",
                                    StageOrder = Convert.ToInt32(reader["stage_order"]),
                                    DefaultProbability = Convert.ToInt32(reader["default_probability"]),
                                    Description = reader["description"]?.ToString() ?? "",
                                    IsClosed = Convert.ToBoolean(reader["is_closed"]),
                                    StageColor = reader["stage_color"]?.ToString() ?? "#6b7280",
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
                Console.WriteLine("Error getting stages: " + ex.Message);
            }

            return stages;
        }

        // Get pipeline metrics
        public Dictionary<string, object> GetPipelineMetrics()
        {
            var metrics = new Dictionary<string, object>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Total pipeline value (all non-archived opportunities)
                    string valueQuery = @"SELECT ISNULL(SUM(deal_value), 0)
                                          FROM tbl_Sales_Pipeline
                                          WHERE is_archived = 0";
                    using (SqlCommand cmd = new SqlCommand(valueQuery, conn))
                    {
                        metrics["TotalValue"] = cmd.ExecuteScalar() ?? 0;
                    }

                    // Active deals count (non-archived opportunities in non-closed stages)
                    string countQuery = @"SELECT COUNT(*)
                                          FROM tbl_Sales_Pipeline sp
                                          INNER JOIN tbl_Pipeline_Stages ps ON sp.stage_id = ps.stage_id
                                          WHERE sp.is_archived = 0 AND ps.is_closed = 0";
                    using (SqlCommand cmd = new SqlCommand(countQuery, conn))
                    {
                        metrics["ActiveDeals"] = cmd.ExecuteScalar() ?? 0;
                    }

                    // Average deal size (all non-archived opportunities)
                    string avgQuery = @"SELECT ISNULL(AVG(deal_value), 0)
                                        FROM tbl_Sales_Pipeline
                                        WHERE is_archived = 0";
                    using (SqlCommand cmd = new SqlCommand(avgQuery, conn))
                    {
                        metrics["AvgDealSize"] = cmd.ExecuteScalar() ?? 0;
                    }

                    // Win rate (percentage of opportunities in closed stages)
                    string winRateQuery = @"SELECT 
                                              CASE WHEN COUNT(*) > 0 
                                                   THEN CAST(SUM(CASE WHEN ps.is_closed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2))
                                                   ELSE 0 
                                              END
                                            FROM tbl_Sales_Pipeline sp
                                            INNER JOIN tbl_Pipeline_Stages ps ON sp.stage_id = ps.stage_id
                                            WHERE sp.is_archived = 0";
                    using (SqlCommand cmd = new SqlCommand(winRateQuery, conn))
                    {
                        metrics["WinRate"] = cmd.ExecuteScalar() ?? 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting pipeline metrics: " + ex.Message);
                metrics["TotalValue"] = 0;
                metrics["ActiveDeals"] = 0;
                metrics["AvgDealSize"] = 0;
                metrics["WinRate"] = 0;
            }

            return metrics;
        }

        // Helper method to map data reader to SalesPipelineModel object
        private SalesPipelineModel MapOpportunityFromReader(SqlDataReader reader)
        {
            return new SalesPipelineModel
            {
                OpportunityId = Convert.ToInt32(reader["opportunity_id"]),
                CustomerId = Convert.ToInt32(reader["customer_id"]),
                AssignedTo = Convert.ToInt32(reader["assigned_to"]),
                StageId = Convert.ToInt32(reader["stage_id"]),
                OpportunityName = reader["opportunity_name"]?.ToString() ?? "",
                DealValue = Convert.ToDecimal(reader["deal_value"]),
                Probability = Convert.ToInt32(reader["probability"]),
                ExpectedCloseDate = reader["expected_close_date"] != DBNull.Value
                    ? Convert.ToDateTime(reader["expected_close_date"]) : (DateTime?)null,
                ActualCloseDate = reader["actual_close_date"] != DBNull.Value
                    ? Convert.ToDateTime(reader["actual_close_date"]) : (DateTime?)null,
                CloseReason = reader["close_reason"]?.ToString() ?? "",
                Notes = reader["notes"]?.ToString() ?? "",
                IsArchived = Convert.ToBoolean(reader["is_archived"]),
                ArchivedDate = reader["archived_date"] != DBNull.Value
                    ? Convert.ToDateTime(reader["archived_date"]) : (DateTime?)null,
                CreatedDate = Convert.ToDateTime(reader["created_date"]),
                ModifiedDate = Convert.ToDateTime(reader["modified_date"]),
                CustomerName = reader["customer_name"]?.ToString() ?? "",
                CompanyName = reader["company_name"]?.ToString() ?? "",
                StageName = reader["stage_name"]?.ToString() ?? "",
                StageOrder = Convert.ToInt32(reader["stage_order"]),
                StageColor = reader["stage_color"]?.ToString() ?? "#6b7280",
                AssignedToName = reader["assigned_to_name"]?.ToString() ?? ""
            };
        }
    }
}