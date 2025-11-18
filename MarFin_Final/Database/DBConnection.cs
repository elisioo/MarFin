using Microsoft.Data.SqlClient;
using System;

namespace MarFin_Final.Data
{
    public class DBConnection
    {

        private static string strConnString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DB_CRM_MarFin;Integrated Security=True";

        public static SqlConnection GetConnection()
        {
            try
            {
                SqlConnection conn = new SqlConnection(strConnString);
                return conn;
            }
            catch (Exception ex)
            {
                throw new Exception("Error connecting to database: " + ex.Message);
            }
        }

        // Method to test the connection
        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}