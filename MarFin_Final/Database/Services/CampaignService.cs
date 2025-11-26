using MarFin_Final.Data;
using Microsoft.Data.SqlClient;
using MarFin_Final.Models;
using System;
using System.Collections.Generic;

namespace MarFin_Final.Services
{
    public class CampaignService
    {
        // Paginated Result Class
        public class PaginatedCampaignResult
        {
            public List<Campaign> Campaigns { get; set; } = new();
            public int TotalRecords { get; set; }
        }

        // CREATE
        public bool AddCampaign(Campaign campaign)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO tbl_Campaigns 
                                   (created_by, campaign_name, campaign_type, start_date, end_date, 
                                    budget, actual_spend, subject_line, email_content, campaign_status,
                                    total_sent, total_opened, total_clicked, total_converted, revenue_generated,
                                    is_archived, created_date, modified_date) 
                                   VALUES 
                                   (@CreatedBy, @CampaignName, @CampaignType, @StartDate, @EndDate,
                                    @Budget, @ActualSpend, @SubjectLine, @EmailContent, @CampaignStatus,
                                    @TotalSent, @TotalOpened, @TotalClicked, @TotalConverted, @RevenueGenerated,
                                    0, GETDATE(), GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CreatedBy", campaign.CreatedBy);
                        cmd.Parameters.AddWithValue("@CampaignName", campaign.CampaignName);
                        cmd.Parameters.AddWithValue("@CampaignType", campaign.CampaignType);
                        cmd.Parameters.AddWithValue("@StartDate", campaign.StartDate);
                        cmd.Parameters.AddWithValue("@EndDate", campaign.EndDate);
                        cmd.Parameters.AddWithValue("@Budget", campaign.Budget);
                        cmd.Parameters.AddWithValue("@ActualSpend", campaign.ActualSpend);
                        cmd.Parameters.AddWithValue("@SubjectLine", (object?)campaign.SubjectLine ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailContent", (object?)campaign.EmailContent ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CampaignStatus", campaign.CampaignStatus);
                        cmd.Parameters.AddWithValue("@TotalSent", campaign.TotalSent);
                        cmd.Parameters.AddWithValue("@TotalOpened", campaign.TotalOpened);
                        cmd.Parameters.AddWithValue("@TotalClicked", campaign.TotalClicked);
                        cmd.Parameters.AddWithValue("@TotalConverted", campaign.TotalConverted);
                        cmd.Parameters.AddWithValue("@RevenueGenerated", campaign.RevenueGenerated);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding campaign: " + ex.Message);
                return false;
            }
        }

        // READ WITH PAGINATION AND FILTERS
        public PaginatedCampaignResult GetCampaignsPaginated(
            int pageNumber,
            int pageSize,
            string searchTerm = "",
            string statusFilter = "",
            string typeFilter = "",
            bool isArchived = false)
        {
            var result = new PaginatedCampaignResult();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Build WHERE clause
                    var whereConditions = new List<string>();
                    whereConditions.Add($"c.is_archived = {(isArchived ? 1 : 0)}");

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        whereConditions.Add("(c.campaign_name LIKE @SearchTerm OR c.subject_line LIKE @SearchTerm)");
                    }

                    if (!string.IsNullOrWhiteSpace(statusFilter))
                    {
                        whereConditions.Add("c.campaign_status = @StatusFilter");
                    }

                    if (!string.IsNullOrWhiteSpace(typeFilter))
                    {
                        whereConditions.Add("c.campaign_type = @TypeFilter");
                    }

                    string whereClause = string.Join(" AND ", whereConditions);

                    // Get total count
                    string countQuery = $@"
                        SELECT COUNT(*) 
                        FROM tbl_Campaigns c
                        WHERE {whereClause}";

                    using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                    {
                        AddFilterParameters(countCmd, searchTerm, statusFilter, typeFilter);
                        result.TotalRecords = (int)countCmd.ExecuteScalar();
                    }

                    // Get paginated data - ordered by created_date DESC (most recent first)
                    string dataQuery = $@"
                        SELECT c.*, u.first_name + ' ' + u.last_name AS created_by_name
                        FROM tbl_Campaigns c
                        INNER JOIN tbl_Users u ON c.created_by = u.user_id
                        WHERE {whereClause}
                        ORDER BY c.created_date DESC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand dataCmd = new SqlCommand(dataQuery, conn))
                    {
                        AddFilterParameters(dataCmd, searchTerm, statusFilter, typeFilter);
                        dataCmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (SqlDataReader reader = dataCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Campaigns.Add(MapCampaignFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting paginated campaigns: " + ex.Message);
            }

            return result;
        }

        private void AddFilterParameters(SqlCommand cmd, string searchTerm, string statusFilter, string typeFilter)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                cmd.Parameters.AddWithValue("@StatusFilter", statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                cmd.Parameters.AddWithValue("@TypeFilter", typeFilter);
            }
        }

        // READ ALL (kept for backwards compatibility)
        public List<Campaign> GetAllCampaigns()
        {
            List<Campaign> campaigns = new List<Campaign>();
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT c.*, u.first_name + ' ' + u.last_name AS created_by_name
                                   FROM tbl_Campaigns c
                                   INNER JOIN tbl_Users u ON c.created_by = u.user_id
                                   WHERE c.is_archived = 0
                                   ORDER BY c.created_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            campaigns.Add(MapCampaignFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting campaigns: " + ex.Message);
            }
            return campaigns;
        }

        // READ ARCHIVED
        public List<Campaign> GetArchivedCampaigns()
        {
            List<Campaign> campaigns = new List<Campaign>();
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT c.*, u.first_name + ' ' + u.last_name AS created_by_name
                                   FROM tbl_Campaigns c
                                   INNER JOIN tbl_Users u ON c.created_by = u.user_id
                                   WHERE c.is_archived = 1
                                   ORDER BY c.archived_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            campaigns.Add(MapCampaignFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting archived campaigns: " + ex.Message);
            }
            return campaigns;
        }

        // READ BY ID
        public Campaign? GetCampaignById(int campaignId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT c.*, u.first_name + ' ' + u.last_name AS created_by_name
                                   FROM tbl_Campaigns c
                                   INNER JOIN tbl_Users u ON c.created_by = u.user_id
                                   WHERE c.campaign_id = @CampaignId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CampaignId", campaignId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapCampaignFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting campaign: " + ex.Message);
            }
            return null;
        }

        // UPDATE
        public bool UpdateCampaign(Campaign campaign)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Campaigns 
                                   SET campaign_name = @CampaignName,
                                       campaign_type = @CampaignType,
                                       start_date = @StartDate,
                                       end_date = @EndDate,
                                       budget = @Budget,
                                       actual_spend = @ActualSpend,
                                       subject_line = @SubjectLine,
                                       email_content = @EmailContent,
                                       campaign_status = @CampaignStatus,
                                       total_sent = @TotalSent,
                                       total_opened = @TotalOpened,
                                       total_clicked = @TotalClicked,
                                       total_converted = @TotalConverted,
                                       revenue_generated = @RevenueGenerated,
                                       modified_date = GETDATE()
                                   WHERE campaign_id = @CampaignId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CampaignId", campaign.CampaignId);
                        cmd.Parameters.AddWithValue("@CampaignName", campaign.CampaignName);
                        cmd.Parameters.AddWithValue("@CampaignType", campaign.CampaignType);
                        cmd.Parameters.AddWithValue("@StartDate", campaign.StartDate);
                        cmd.Parameters.AddWithValue("@EndDate", campaign.EndDate);
                        cmd.Parameters.AddWithValue("@Budget", campaign.Budget);
                        cmd.Parameters.AddWithValue("@ActualSpend", campaign.ActualSpend);
                        cmd.Parameters.AddWithValue("@SubjectLine", (object?)campaign.SubjectLine ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailContent", (object?)campaign.EmailContent ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CampaignStatus", campaign.CampaignStatus);
                        cmd.Parameters.AddWithValue("@TotalSent", campaign.TotalSent);
                        cmd.Parameters.AddWithValue("@TotalOpened", campaign.TotalOpened);
                        cmd.Parameters.AddWithValue("@TotalClicked", campaign.TotalClicked);
                        cmd.Parameters.AddWithValue("@TotalConverted", campaign.TotalConverted);
                        cmd.Parameters.AddWithValue("@RevenueGenerated", campaign.RevenueGenerated);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating campaign: " + ex.Message);
                return false;
            }
        }

        // ARCHIVE
        public bool ArchiveCampaign(int campaignId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Campaigns 
                                   SET is_archived = 1, 
                                       archived_date = GETDATE()
                                   WHERE campaign_id = @CampaignId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CampaignId", campaignId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error archiving campaign: " + ex.Message);
                return false;
            }
        }

        // RESTORE
        public bool RestoreCampaign(int campaignId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Campaigns 
                                   SET is_archived = 0, 
                                       archived_date = NULL
                                   WHERE campaign_id = @CampaignId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CampaignId", campaignId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error restoring campaign: " + ex.Message);
                return false;
            }
        }

        // GET SUMMARY METRICS
        public Dictionary<string, object> GetCampaignMetrics()
        {
            var metrics = new Dictionary<string, object>();
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT 
                                    ISNULL(SUM(total_sent), 0) AS TotalSent,
                                    CASE WHEN SUM(total_sent) > 0 
                                        THEN ROUND(CAST(SUM(total_opened) AS FLOAT) / SUM(total_sent) * 100, 2)
                                        ELSE 0 END AS AvgOpen,
                                    CASE WHEN SUM(total_sent) > 0 
                                        THEN ROUND(CAST(SUM(total_clicked) AS FLOAT) / SUM(total_sent) * 100, 2)
                                        ELSE 0 END AS AvgClick,
                                    ISNULL(SUM(revenue_generated), 0) AS TotalROI
                                   FROM tbl_Campaigns 
                                   WHERE is_archived = 0";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            metrics["TotalSent"] = reader["TotalSent"];
                            metrics["AvgOpen"] = reader["AvgOpen"];
                            metrics["AvgClick"] = reader["AvgClick"];
                            metrics["TotalROI"] = reader["TotalROI"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting metrics: " + ex.Message);
            }
            return metrics;
        }

        private Campaign MapCampaignFromReader(SqlDataReader reader)
        {
            return new Campaign
            {
                CampaignId = Convert.ToInt32(reader["campaign_id"]),
                CreatedBy = Convert.ToInt32(reader["created_by"]),
                CampaignName = reader["campaign_name"]?.ToString() ?? "",
                CampaignType = reader["campaign_type"]?.ToString() ?? "Email",
                StartDate = Convert.ToDateTime(reader["start_date"]),
                EndDate = Convert.ToDateTime(reader["end_date"]),
                Budget = Convert.ToDecimal(reader["budget"]),
                ActualSpend = Convert.ToDecimal(reader["actual_spend"]),
                SubjectLine = reader["subject_line"]?.ToString(),
                EmailContent = reader["email_content"]?.ToString(),
                CampaignStatus = reader["campaign_status"]?.ToString() ?? "Draft",
                TotalSent = Convert.ToInt32(reader["total_sent"]),
                TotalOpened = Convert.ToInt32(reader["total_opened"]),
                TotalClicked = Convert.ToInt32(reader["total_clicked"]),
                TotalConverted = Convert.ToInt32(reader["total_converted"]),
                RevenueGenerated = Convert.ToDecimal(reader["revenue_generated"]),
                IsArchived = Convert.ToBoolean(reader["is_archived"]),
                ArchivedDate = reader["archived_date"] != DBNull.Value ? Convert.ToDateTime(reader["archived_date"]) : null,
                CreatedDate = Convert.ToDateTime(reader["created_date"]),
                ModifiedDate = Convert.ToDateTime(reader["modified_date"]),
                CreatedByName = reader["created_by_name"]?.ToString() ?? ""
            };
        }
    }
}