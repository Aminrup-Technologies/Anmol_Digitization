using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public class DatabaseHelper
    {
        // Get the connection string from web.config
        private static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        // Establish a connection to the database
        public static SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(GetConnectionString());
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                // Handle connection error, if any
                // You might want to log the exception or take other actions
                throw ex;
            }
            return connection;
        }

        public static void BindWithDefaultNoRecords(DropDownList ddl)
        {
            // Clear existing items and add the default "No Records Found" item
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem("No Records Found", "0"));
        }


        public static void BindDropDownList1(string query, DropDownList ddl, string textField, string valueField)
        {
            using (SqlConnection connection = GetConnection())
            {
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                ddl.DataSource = reader;
                ddl.DataTextField = textField;
                ddl.DataValueField = valueField;
                ddl.DataBind();

                reader.Close();
                connection.Close();
            }

            // Add a default item to the DropDownList
            ddl.Items.Insert(0, new ListItem("Select", "0"));
        }

        public static void BindDropDownList(string query, DropDownList ddl, string textField, string valueField, out bool recordsBound)
        {
            // Initialize the flag
            recordsBound = false;

            using (SqlConnection connection = GetConnection())
            {
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                // Check if there are any records to bind
                if (reader.HasRows)
                {
                    ddl.DataSource = reader;
                    ddl.DataTextField = textField;
                    ddl.DataValueField = valueField;
                    ddl.DataBind();

                    // Set the flag to indicate that records were bound
                    recordsBound = true;
                }

                reader.Close();
                connection.Close();
            }

            // Add a default item to the DropDownList
            ddl.Items.Insert(0, new ListItem("Select", "0"));
        }



        // Overload method with parameterized query
        public static void BindDropDownList1(string query, DropDownList ddl, string textField, string valueField, SqlParameter parameter)
        {
            // Create a connection and command
            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(parameter);
                    SqlDataReader reader = command.ExecuteReader();
                    ddl.DataSource = reader;
                    ddl.DataTextField = textField;
                    ddl.DataValueField = valueField;
                    ddl.DataBind();
                    reader.Close();
                    connection.Close();
                }
            }
        }

        public static void BindDropDownList2(string query, DropDownList ddl, string textField, string valueField, SqlParameter parameter, out bool recordsBound)
        {
            // Initialize the flag
            recordsBound = false;

            // Create a connection and command
            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add the parameter to the command
                    command.Parameters.Add(parameter);

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        // Check if there are any records to bind
                        if (reader.HasRows)
                        {
                            ddl.DataSource = reader;
                            ddl.DataTextField = textField;
                            ddl.DataValueField = valueField;
                            ddl.DataBind();

                            // Set the flag to indicate that records were bound
                            recordsBound = true;
                        }

                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., log the error, display a message)
                        // For simplicity, you can just rethrow the exception
                        throw new Exception("An error occurred while executing the query: " + ex.Message, ex);
                    }
                }
            }
        }


        public static void BindDropDownList(string query, DropDownList ddl, string textField, string valueField, SqlParameter parameter, out bool recordsBound)
        {
            // Initialize the flag
            recordsBound = false;

            // Create a connection and command
            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add the parameter to the command
                    command.Parameters.Add(parameter);

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        // Check if there are any records to bind
                        if (reader.HasRows)
                        {
                            ddl.DataSource = reader;
                            ddl.DataTextField = textField;
                            ddl.DataValueField = valueField;
                            ddl.DataBind();

                            // Set the flag to indicate that records were bound
                            recordsBound = true;
                        }
                        else
                        {
                            // If no records found, bind a default record
                            ddl.Items.Clear();
                            ddl.Items.Add(new ListItem("No records found", ""));
                        }
                        connection.Close();
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        // Handle the exception (e.g., log the error, display a message)
                        // For simplicity, you can just rethrow the exception
                        throw new Exception("An error occurred while executing the query: " + ex.Message, ex);
                    }

                }
            }
            // Add a default item to the DropDownList
            ddl.Items.Insert(0, new ListItem("Select", "0"));
        }

        //public static DataTable GetBrandFieldsControlByMaterialIdAndPlantId(int materialId, int brandId)
        //{
        //    DataTable dataTable = new DataTable();

        //    // Create a SqlConnection
        //    using (SqlConnection connection = GetConnection())
        //    {
        //        // Open the connection

        //        // Create a SqlCommand for the stored procedure
        //        using (SqlCommand command = new SqlCommand("GetRMFieldsControlByPlantId", connection))
        //        {
        //            command.CommandType = CommandType.StoredProcedure;

        //            // Add parameters
        //            command.Parameters.AddWithValue("@material_id", materialId);
        //            command.Parameters.AddWithValue("@plant_id", brandId);

        //            // Execute the SqlCommand and load results into the DataTable
        //            using (SqlDataReader reader = command.ExecuteReader())
        //            {
        //                dataTable.Load(reader);
        //            }
        //        }
        //        connection.Close();
        //    }

        //    return dataTable;
        //}

        public static DataTable GetBrandFieldsControlByMaterialIdAndPlantId(int materialId, int brandId)
        {
            DataTable dataTable = new DataTable();

            // Create a SqlConnection
            using (SqlConnection connection = GetConnection())
            {
                // Open the connection

                // Create a SqlCommand for the stored procedure
                using (SqlCommand command = new SqlCommand("GetRMFieldsControlByPlantId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@material_id", materialId);
                    command.Parameters.AddWithValue("@plant_id", brandId);

                    // Execute the SqlCommand and load results into the DataTable
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
                connection.Close();
            }

            return dataTable;
        }


        public static void BindDropDownList(string query, DropDownList ddl, string textField, string valueField, SqlParameter[] parameters, out bool recordsBound)
        {
            // Initialize the flag
            recordsBound = false;

            // Create a connection and command
            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameters to the command
                    command.Parameters.AddRange(parameters);

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        // Check if there are any records to bind
                        if (reader.HasRows)
                        {
                            ddl.DataSource = reader;
                            ddl.DataTextField = textField;
                            ddl.DataValueField = valueField;
                            ddl.DataBind();

                            // Set the flag to indicate that records were bound
                            recordsBound = true;
                        }

                        else
                        {
                            // If no records found, bind a default record
                            ddl.Items.Clear();
                            ddl.Items.Add(new ListItem("No records found", ""));
                            ddl.DataBind(); // Ensure the default record is displayed
                        }

                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., log the error, display a message)
                        // For simplicity, you can just rethrow the exception
                        throw new Exception("An error occurred while executing the query: " + ex.Message, ex);
                    }
                }
            }
            // Add a default item to the DropDownList
            ddl.Items.Insert(0, new ListItem("Select", "0"));
        }


        public static DataTable GetBrandFieldsControlByBrandId(int brandId)
        {
            DataTable dataTable = new DataTable();

            // Create a SqlConnection
            using (SqlConnection connection = GetConnection())
            {
                // Open the connection

                // Create a SqlCommand for the stored procedure
                using (SqlCommand command = new SqlCommand("GetBrandFieldsControlByBrandId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@brand_id", brandId);

                    // Execute the SqlCommand and load results into the DataTable
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
                connection.Close();
            }

            return dataTable;
        }



        public static void BindLiteralControl(string query, Literal span_L, Literal span_W, Literal span_H, Literal span_GSM, SqlParameter parameter, out bool recordsBound)
        {
            // Initialize the output parameter
            recordsBound = false;

            // Create a DataTable to hold the query results
            DataTable dataTable = new DataTable();

            try
            {

                using (SqlConnection conn = GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add(parameter);

                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                        //conn.Open();
                        adapter.Fill(dataTable);

                        // Check if data is available
                        recordsBound = dataTable.Rows.Count > 0;

                        // If records are bound, populate the Literal controls
                        //if (recordsBound)
                        //{
                        //    DataRow row = dataTable.Rows[0];

                        //    // Set text for Literal controls
                        //    if (span_L != null) span_W.Text = row["Dimension_Std_W"].ToString();
                        //    if (span_W != null) span_L.Text = row["Dimension_Std_L"].ToString();
                        //    if (span_H != null) span_H.Text = row["Dimension_Std_H"].ToString();
                        //    if (span_GSM != null) span_GSM.Text = row["GMS_Std"].ToString();
                        //}
                        if (recordsBound && dataTable.Rows.Count > 0)
                        {
                            DataRow row = dataTable.Rows[0];

                            // Set text for Literal controls or default to "0.00"
                            span_L.Text = row["Dimension_Std_L"]?.ToString() ?? "0.00";
                            span_W.Text = row["Dimension_Std_W"]?.ToString() ?? "0.00";
                            span_H.Text = row["Dimension_Std_H"]?.ToString() ?? "0.00";
                            span_GSM.Text = row["GMS_Std"]?.ToString() ?? "0.00";
                        }
                        else
                        {
                            // Default values for Literal controls when records are not found
                            span_L.Text = "0.00";
                            span_W.Text = "0.00";
                            span_H.Text = "0.00";
                            span_GSM.Text = "0.00";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log the error, display a message)
                // For simplicity, you can just rethrow the exception
                throw new Exception("An error occurred while executing the query: " + ex.Message, ex);
            }
        }


        public static void BindLiteralControl(string query, Literal span_Dimension, Literal span_GSM, SqlParameter parameter, out bool recordsBound)
        {
            // Initialize the output parameter
            recordsBound = false;

            // Create a DataTable to hold the query results
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection conn = GetConnection())  // Assumes GetConnection() provides a valid SqlConnection
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Add the provided parameter to the SQL command
                        cmd.Parameters.Add(parameter);

                        // Use SqlDataAdapter to execute the query and fill the DataTable
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(dataTable);

                        // Check if any data was returned
                        recordsBound = dataTable.Rows.Count > 0;

                        // If records are bound, populate the Literal controls
                        if (recordsBound)
                        {
                            DataRow row = dataTable.Rows[0];  // Get the first row of results

                            // Populate the Literal controls if they are not null
                            if (span_Dimension != null) span_Dimension.Text = row["Dimension_Std"].ToString();
                            if (span_GSM != null) span_GSM.Text = row["GMS_Std"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., logging or rethrowing the exception with additional context
                throw new Exception("An error occurred while executing the query: " + ex.Message, ex);
            }
        }
    }
}