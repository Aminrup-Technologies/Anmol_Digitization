using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Web.UI.WebControls;
using Microsoft.VisualBasic.FileIO;

namespace AnmolDristi
{
    public partial class HrmsDataImport : BasePage
    {
        // Define the exact columns your DB Staging Table expects
        private readonly string[] ExpectedDbColumns = new string[]
        {
            // Core
            "EmpCode", "EmpName", "EmpStatus", "EmployeeStatus", "Email", "MobileNo",
            "DOJ", "Gender", // <--- ADDED HERE
            // Master IDs & Descriptions
            "Branch_ID", "Branch_Description",
            "Department_ID", "Department_Description",
            "Designation_ID", "Designation_Description",
            "Grade_ID", "Grade_Description",
            "PlantID_ID", "PlantID_Description",
            "Region_ID", "Region_Description",
            "Division_ID", "Division_Description",
            "Category_ID", "Category_Description",  // <-- Added here!
            "Company_ID", "Company_Description",
            "SubDepartment_ID",
    
            // Managers
            "ReportingManager", "ReportingManagerCode",
            "ReportingManagersManagerName", "ReportingManagersManagerCode"
        };

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // 1. Check if the user is authenticated
            if (Session["UserId"] == null || string.IsNullOrEmpty(Session["UserId"].ToString()))
            {
                // Session expired or user not logged in. Redirect immediately.
                // Appending a query string helps the Login page show a "Session Expired" message if desired.
                Response.Redirect("~/Login.aspx?msg=session_expired", true);
                return; // Ensure no further page lifecycle events fire
            }

            if (!IsPostBack)
            {
                // Any initial data binding goes here
            }
        }

        protected void btnReadHeaders_Click(object sender, EventArgs e)
        {
            if (!fileUploadCsv.HasFile)
            {
                ShowMessage("Please select a CSV file to upload.", false);
                return;
            }

            try
            {
                // Save file temporarily to server so we can process it in Step 2 without re-uploading
                string tempPath = Server.MapPath("~/TempUploads/");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                string uniqueFileName = string.Format("{0}_{1}", Guid.NewGuid().ToString("N"), fileUploadCsv.FileName);
                string fullPath = Path.Combine(tempPath, uniqueFileName);
                fileUploadCsv.SaveAs(fullPath);

                // Save path to session for Step 2
                Session["TempCsvPath"] = fullPath;
                Session["OriginalFileName"] = fileUploadCsv.FileName;

                // Read headers
                List<string> excelHeaders = new List<string>();
                using (StreamReader sr = new StreamReader(fullPath))
                using (TextFieldParser parser = new TextFieldParser(sr))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    if (!parser.EndOfData)
                    {
                        string[] headers = parser.ReadFields();
                        foreach (string h in headers)
                        {
                            excelHeaders.Add(h.Trim());
                        }
                    }
                }

                Session["ExcelHeaders"] = excelHeaders;

                // Bind Grid
                DataTable dtMapping = new DataTable();
                dtMapping.Columns.Add("DbColumn", typeof(string));
                foreach (string col in ExpectedDbColumns)
                {
                    dtMapping.Rows.Add(col);
                }

                rptMapping.DataSource = dtMapping;
                rptMapping.DataBind();

                pnlUpload.Visible = false;
                pnlMapping.Visible = true;
                lblMessage.Text = string.Empty;
            }
            catch (Exception ex)
            {
                ShowMessage(string.Format("Error reading file: {0}", ex.Message), false);
            }
        }

        protected void rptMapping_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DropDownList ddlExcelColumn = (DropDownList)e.Item.FindControl("ddlExcelColumn");
                HiddenField hfDbColumn = (HiddenField)e.Item.FindControl("hfDbColumn");

                List<string> excelHeaders = Session["ExcelHeaders"] as List<string>;

                ddlExcelColumn.Items.Add(new ListItem("-- Ignore / Do Not Map --", ""));

                if (excelHeaders != null)
                {
                    foreach (string header in excelHeaders)
                    {
                        ddlExcelColumn.Items.Add(new ListItem(header, header));
                    }

                    // Auto-select logic: If DB column name perfectly matches Excel Header name
                    ListItem matchedItem = ddlExcelColumn.Items.FindByText(hfDbColumn.Value);
                    if (matchedItem != null)
                    {
                        ddlExcelColumn.SelectedValue = matchedItem.Value;
                    }
                }
            }
        }

        protected void btnProcess_Click(object sender, EventArgs e)
        {
            string tempPath = Session["TempCsvPath"] as string;

            if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
            {
                ShowMessage("Session expired or file lost. Please upload again.", false);
                btnCancel_Click(null, null);
                return;
            }

            // 1. Build Mapping Dictionary from User's UI Selections
            Dictionary<string, string> dbToExcelMap = new Dictionary<string, string>();
            foreach (RepeaterItem item in rptMapping.Items)
            {
                HiddenField hfDbColumn = (HiddenField)item.FindControl("hfDbColumn");
                DropDownList ddlExcelColumn = (DropDownList)item.FindControl("ddlExcelColumn");

                if (!string.IsNullOrEmpty(ddlExcelColumn.SelectedValue))
                {
                    dbToExcelMap.Add(hfDbColumn.Value, ddlExcelColumn.SelectedValue);
                }
            }

            string connectionString = GetConnectionString();

            try
            {
                // 2. Create Batch ID
                int batchId = 0;
                string uploadedBy = Session["UserId"] != null ? Session["UserId"].ToString() : "UnknownSystemUser";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string insertBatchSql = "INSERT INTO dbo.ImportBatchMaster (FileName, UploadedBy) OUTPUT INSERTED.BatchId VALUES (@FileName, @UploadedBy)";
                    using (SqlCommand cmd = new SqlCommand(insertBatchSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FileName", Session["OriginalFileName"].ToString());
                        cmd.Parameters.AddWithValue("@UploadedBy", uploadedBy);
                        conn.Open();
                        batchId = (int)cmd.ExecuteScalar();
                    }
                }

                // 3. Read CSV and Apply Dynamic User Mapping
                DataTable dtStaging = CreateStagingTable();

                using (StreamReader sr = new StreamReader(tempPath))
                using (TextFieldParser parser = new TextFieldParser(sr))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    // Header Map Dictionary: Excel Header Name -> Array Index
                    Dictionary<string, int> headerIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (!parser.EndOfData)
                    {
                        string[] headers = parser.ReadFields();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (!headerIndexMap.ContainsKey(headers[i].Trim()))
                                headerIndexMap.Add(headers[i].Trim(), i);
                        }
                    }

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        DataRow row = dtStaging.NewRow();
                        row["BatchId"] = batchId;

                        // Use the UI Map to pull the correct Excel index into the DB column
                        foreach (KeyValuePair<string, string> mapping in dbToExcelMap)
                        {
                            string dbCol = mapping.Key;
                            string excelCol = mapping.Value;

                            if (headerIndexMap.ContainsKey(excelCol))
                            {
                                int index = headerIndexMap[excelCol];
                                if (fields.Length > index)
                                {
                                    row[dbCol] = fields[index].Trim();
                                }
                            }
                        }

                        dtStaging.Rows.Add(row);

                        if (dtStaging.Rows.Count >= 5000)
                        {
                            InsertBatch(dtStaging, connectionString);
                            dtStaging.Clear();
                        }
                    }

                    if (dtStaging.Rows.Count > 0)
                    {
                        InsertBatch(dtStaging, connectionString);
                    }
                }

                // 4. Run Stored Procedure
                ProcessBatch(batchId, chkAllowOverwrite.Checked, connectionString);

                // Clean up Temp File
                File.Delete(tempPath);

                // Reset UI
                btnCancel_Click(null, null);
            }
            catch (Exception ex)
            {
                // Log the actual exception to your server logs here (NLog, Serilog, etc.)
                ShowMessage(string.Format("A critical error occurred: {0}", ex.Message), false);
            }
            finally
            {
                // GUARANTEED CLEANUP: This runs no matter what happens above.
                try
                {
                    if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                    {
                        // Force garbage collection to release file handles if TextFieldParser kept it open
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        File.Delete(tempPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    // Failsafe: If deletion fails (e.g., file locked by another process), 
                    // catch it so it doesn't crash the IIS worker process. 
                    // In a real app, log this cleanupEx to your error logger.
                    System.Diagnostics.Debug.WriteLine("Failed to delete temp file: " + cleanupEx.Message);
                }
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlUpload.Visible = true;
            pnlMapping.Visible = false;
            Session["TempCsvPath"] = null;
            Session["ExcelHeaders"] = null;
        }

        private DataTable CreateStagingTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("BatchId", typeof(int));
            foreach (string col in ExpectedDbColumns)
            {
                dt.Columns.Add(col, typeof(string));
            }
            return dt;
        }

        private void InsertBatch(DataTable dt, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "dbo.Staging_EmployeeData";
                            bulkCopy.BulkCopyTimeout = 300;

                            foreach (DataColumn col in dt.Columns)
                            {
                                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            }
                            bulkCopy.WriteToServer(dt);
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception(string.Format("Bulk insert failed: {0}", ex.Message));
                    }
                }
            }
        }

        //private void ProcessBatch(int batchId, bool allowOverwrite, string connectionString)
        //{
        //    int processedCount = 0, errorCount = 0, skippedCount = 0;

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("dbo.usp_ProcessEmployeeBatch_V2", conn))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.CommandTimeout = 300;
        //            cmd.Parameters.AddWithValue("@BatchId", batchId);
        //            cmd.Parameters.AddWithValue("@AllowOverwrite", allowOverwrite);

        //            conn.Open();
        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    processedCount = reader["ProcessedCount"] != DBNull.Value ? Convert.ToInt32(reader["ProcessedCount"]) : 0;
        //                    errorCount = reader["ErrorCount"] != DBNull.Value ? Convert.ToInt32(reader["ErrorCount"]) : 0;
        //                    skippedCount = reader["SkippedCount"] != DBNull.Value ? Convert.ToInt32(reader["SkippedCount"]) : 0;
        //                }
        //            }
        //        }
        //    }

        //    ShowMessage(string.Format("Import Successful! Processed: {0}, Errors: {1}, Skipped: {2}", processedCount, errorCount, skippedCount), true);
        //}

        private void ProcessBatch(int batchId, bool allowOverwrite, string connectionString)
        {
            // Temporary values until V2 is completed
            int totalRecords = 0;
            int insertedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_RunHRMSSynchronization", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300;

                cmd.Parameters.AddWithValue("@BatchId", batchId);

                // Legacy parameter (wrapper still accepts it)
                cmd.Parameters.AddWithValue("@AllowOverwrite", allowOverwrite);

                // New parameters
                cmd.Parameters.AddWithValue("@ProcessingMode", 0);              // Insert Only
                cmd.Parameters.AddWithValue("@Modules", "Employees");
                cmd.Parameters.AddWithValue("@OverwriteFields", DBNull.Value);
                cmd.Parameters.AddWithValue(
                    "@ExecutedBy",
                    Session["UserId"] != null
                        ? Session["UserId"].ToString()
                        : "SYSTEM");

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Skip debug dataset (#EmployeeSource)
                    while (reader.NextResult())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (HasColumn(reader, "TotalRecords"))
                                    totalRecords = Convert.ToInt32(reader["TotalRecords"]);

                                if (HasColumn(reader, "InsertedCount"))
                                    insertedCount = Convert.ToInt32(reader["InsertedCount"]);

                                if (HasColumn(reader, "UpdatedCount"))
                                    updatedCount = Convert.ToInt32(reader["UpdatedCount"]);

                                if (HasColumn(reader, "SkippedCount"))
                                    skippedCount = Convert.ToInt32(reader["SkippedCount"]);

                                if (HasColumn(reader, "ErrorCount"))
                                    errorCount = Convert.ToInt32(reader["ErrorCount"]);
                            }

                            break;
                        }
                    }
                }
            }

            ShowMessage(
                string.Format(
                    "Import Completed. Total: {0}, Inserted: {1}, Updated: {2}, Errors: {3}, Skipped: {4}",
                    totalRecords,
                    insertedCount,
                    updatedCount,
                    errorCount,
                    skippedCount),
                true);
        }

        private bool HasColumn(IDataRecord reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void ShowMessage(string message, bool isSuccess)
        {
            lblMessage.Text = message;
            lblMessage.CssClass = isSuccess ? "font-weight-bold d-block mb-3 text-success" : "font-weight-bold d-block mb-3 text-danger";
        }
    }
}