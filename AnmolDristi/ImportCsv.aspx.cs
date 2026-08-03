using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace AnmolDristi
{
    public partial class ImportCsv : AnmolDristi.BasePage // Ensure this inherits your BasePage
    {
        private readonly string _connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        // 5000 is an optimal batch size for ~31,000 rows to balance memory and network I/O
        private const int BulkBatchSize = 5000;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Initial page load setup if needed
            }
        }

        protected void btnUpload_Click_OLD(object sender, EventArgs e)
        {
            // 1. Basic File Validation
            if (!fuCsv.HasFile)
            {
                lblMessage.Text = "Please choose a CSV file.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            string fileExtension = Path.GetExtension(fuCsv.FileName);
            if (!string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                lblMessage.Text = "Invalid file type. Please upload a .csv file.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            var importBatchId = Guid.NewGuid();
            var fileName = fuCsv.FileName;
            var importedByUserId = HttpContext.Current.User.Identity.Name ?? "system";
            var importedByUsername = HttpContext.Current.User.Identity.Name ?? "system";

            int totalRowsProcessed = 0;

            try
            {
                using (var stream = fuCsv.PostedFile.InputStream)
                {
                    if (stream.CanSeek) stream.Position = 0;

                    using (var tfp = new TextFieldParser(stream))
                    {
                        tfp.TextFieldType = FieldType.Delimited;
                        tfp.SetDelimiters(",");
                        tfp.HasFieldsEnclosedInQuotes = true;
                        tfp.TrimWhiteSpace = true;

                        if (tfp.EndOfData)
                        {
                            lblMessage.Text = "The uploaded CSV file is empty.";
                            lblMessage.CssClass = "text-warning";
                            return;
                        }

                        // Read and clean headers
                        string[] headers = tfp.ReadFields();
                        if (headers == null || headers.Length == 0)
                        {
                            lblMessage.Text = "CSV header is missing or unreadable.";
                            lblMessage.CssClass = "text-danger";
                            return;
                        }

                        for (int i = 0; i < headers.Length; i++)
                        {
                            headers[i] = headers[i]?.Trim() ?? string.Empty;
                        }

                        var dtBatch = CreateStagingDataTableSchema();

                        while (!tfp.EndOfData)
                        {
                            string[] fields = null;
                            try
                            {
                                fields = tfp.ReadFields();
                            }
                            catch (MalformedLineException)
                            {
                                // Skip entirely broken lines (e.g., mismatched quotes)
                                continue;
                            }

                            if (fields == null) continue;

                            // Build JSON Dictionary for the Stored Procedure to consume
                            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                string key = headers[i];
                                string val = (i < fields.Length && fields[i] != null) ? fields[i].Trim() : string.Empty;
                                normalized[key] = val;
                            }

                            var newRow = dtBatch.NewRow();

                            // Serialize everything to RawJson for the Stored Procedure
                            newRow["RawJson"] = JsonConvert.SerializeObject(normalized);

                            // Extract explicitly required Staging columns
                            newRow["Zone"] = SafeGet(normalized, "Zone");
                            newRow["Reporting_Manager3"] = SafeGet(normalized, "Reporting_Manager3");
                            newRow["Reporting_Manager2"] = SafeGet(normalized, "Reporting_Manager2");
                            newRow["Reporting_Manager1"] = SafeGet(normalized, "Reporting_Manager1");
                            newRow["Login_Id"] = SafeGet(normalized, "Login_Id");
                            newRow["User_Id"] = SafeGet(normalized, "User_Id");
                            newRow["User_Name"] = SafeGet(normalized, "User_Name");
                            newRow["Month"] = SafeGet(normalized, "Month");
                            newRow["Attendance_Status"] = SafeGet(normalized, "Attendance_Status");

                            // --- Robust Date Parsing (Format from sample: 01-02-2026) ---
                            string dateStr = SafeGet(normalized, "Date") ?? SafeGet(normalized, "User_Created_Date");
                            DateTime parsedDate;
                            if (DateTime.TryParseExact(dateStr, new[] { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd/MM/yyyy" },
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            {
                                newRow["Date"] = parsedDate.Date;
                                newRow["Day"] = parsedDate.DayOfWeek.ToString(); // Auto-calculate correct day
                            }
                            else
                            {
                                newRow["Date"] = DBNull.Value;
                                newRow["Day"] = SafeGet(normalized, "Day") ?? (object)DBNull.Value;
                            }

                            // --- Robust Decimal Parsing ---
                            string kmStr = SafeGet(normalized, "Total_Distance_Travelled_In_KM");
                            decimal kmVal;
                            if (decimal.TryParse(kmStr, NumberStyles.Any, CultureInfo.InvariantCulture, out kmVal))
                                newRow["Total_Distance_Travelled_In_KM"] = kmVal;
                            else
                                newRow["Total_Distance_Travelled_In_KM"] = DBNull.Value; // Let DB handle NULLs

                            // Metadata
                            newRow["ImportedByUserId"] = importedByUserId;
                            newRow["ImportedByUsername"] = importedByUsername;
                            newRow["ImportedDateTime"] = DateTime.Now;
                            newRow["ImportStatus"] = "Inserted";
                            newRow["ImportFileName"] = fileName;
                            newRow["ImportBatchId"] = importBatchId;

                            dtBatch.Rows.Add(newRow);
                            totalRowsProcessed++;

                            // Execute Bulk Insert in batches
                            if (dtBatch.Rows.Count >= BulkBatchSize)
                            {
                                BulkInsertStaging(dtBatch);
                                dtBatch.Clear();
                            }
                        }

                        // Flush remaining rows
                        if (dtBatch.Rows.Count > 0)
                        {
                            BulkInsertStaging(dtBatch);
                            dtBatch.Clear();
                        }
                    }
                }

                // 2. Process Data to Final Table & Get Results Summary
                ProcessStagingToFinal(importBatchId);

                // 3. Bind Preview Data (Shows top 50 successful rows)
                BindPreviewGrid(importBatchId);

                // 4. Bind Error Data (Reveals the Error Grid if any rows failed)
                BindErrorGrid(importBatchId);
            }
            catch (Exception ex)
            {
                lblMessage.Text = "A critical error occurred during import: " + ex.Message;
                lblMessage.CssClass = "text-danger font-weight-bold";
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            // 1. Basic File Validation
            if (!fuCsv.HasFile)
            {
                lblMessage.Text = "Please choose a CSV file.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            // NEW: File Size Validation (Limit to 15MB to protect IIS RAM)
            int maxFileSizeBytes = 15 * 1024 * 1024;
            if (fuCsv.PostedFile.ContentLength > maxFileSizeBytes)
            {
                lblMessage.Text = "File size exceeds the 15MB limit. Please split your file.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            string fileExtension = Path.GetExtension(fuCsv.FileName);
            if (!string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                lblMessage.Text = "Invalid file type. Please upload a .csv file.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            var importBatchId = Guid.NewGuid();
            var fileName = fuCsv.FileName;
            var importedByUserId = HttpContext.Current.User.Identity.Name ?? "system";
            var importedByUsername = HttpContext.Current.User.Identity.Name ?? "system";
            int totalRowsProcessed = 0;

            try
            {
                using (var stream = fuCsv.PostedFile.InputStream)
                {
                    if (stream.CanSeek) stream.Position = 0;

                    using (var tfp = new TextFieldParser(stream))
                    {
                        tfp.TextFieldType = FieldType.Delimited;
                        tfp.SetDelimiters(",");
                        tfp.HasFieldsEnclosedInQuotes = true;
                        tfp.TrimWhiteSpace = true;

                        if (tfp.EndOfData)
                        {
                            lblMessage.Text = "The uploaded CSV file is empty.";
                            lblMessage.CssClass = "text-warning";
                            return;
                        }

                        string[] headers = tfp.ReadFields();
                        if (headers == null || headers.Length == 0)
                        {
                            lblMessage.Text = "CSV header is missing or unreadable.";
                            lblMessage.CssClass = "text-danger";
                            return;
                        }

                        for (int i = 0; i < headers.Length; i++)
                        {
                            headers[i] = headers[i]?.Trim() ?? string.Empty;
                        }

                        var dtBatch = CreateStagingDataTableSchema();

                        // 2. Open ONE Connection and ONE Transaction for the entire file
                        using (var conn = new SqlConnection(_connString))
                        {
                            conn.Open();
                            using (var transaction = conn.BeginTransaction())
                            {
                                try
                                {
                                    while (!tfp.EndOfData)
                                    {
                                        string[] fields = null;
                                        try { fields = tfp.ReadFields(); }
                                        catch (MalformedLineException) { continue; }

                                        if (fields == null) continue;

                                        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                        for (int i = 0; i < headers.Length; i++)
                                        {
                                            string key = headers[i];
                                            string val = (i < fields.Length && fields[i] != null) ? fields[i].Trim() : string.Empty;
                                            normalized[key] = val;
                                        }

                                        var newRow = dtBatch.NewRow();
                                        newRow["RawJson"] = JsonConvert.SerializeObject(normalized);

                                        newRow["Zone"] = SafeGet(normalized, "Zone");
                                        newRow["Reporting_Manager3"] = SafeGet(normalized, "Reporting_Manager3");
                                        newRow["Reporting_Manager2"] = SafeGet(normalized, "Reporting_Manager2");
                                        newRow["Reporting_Manager1"] = SafeGet(normalized, "Reporting_Manager1");
                                        newRow["Login_Id"] = SafeGet(normalized, "Login_Id");
                                        newRow["User_Id"] = SafeGet(normalized, "User_Id");
                                        newRow["User_Name"] = SafeGet(normalized, "User_Name");
                                        newRow["Month"] = SafeGet(normalized, "Month");
                                        newRow["Attendance_Status"] = SafeGet(normalized, "Attendance_Status");

                                        string dateStr = SafeGet(normalized, "Date") ?? SafeGet(normalized, "User_Created_Date");

                                        // 1. Pre-declare the variable for C# 5.0 / .NET 4.5.2 compatibility
                                        DateTime parsedDate;

                                        // 2. Pass the pre-declared variable using just 'out parsedDate'
                                        if (DateTime.TryParseExact(dateStr, new[] { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd/MM/yyyy" },
                                            CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                                        {
                                            newRow["Date"] = parsedDate.Date;
                                            newRow["Day"] = parsedDate.DayOfWeek.ToString();
                                        }
                                        else
                                        {
                                            newRow["Date"] = DBNull.Value;
                                            newRow["Day"] = SafeGet(normalized, "Day") ?? (object)DBNull.Value;
                                        }

                                        string kmStr = SafeGet(normalized, "Total_Distance_Travelled_In_KM");

                                        // Pre-declare the variable for C# 5.0 compatibility
                                        decimal kmVal;

                                        if (decimal.TryParse(kmStr, NumberStyles.Any, CultureInfo.InvariantCulture, out kmVal))
                                        {
                                            newRow["Total_Distance_Travelled_In_KM"] = kmVal;
                                        }
                                        else
                                        {
                                            newRow["Total_Distance_Travelled_In_KM"] = DBNull.Value;
                                        }

                                        newRow["ImportedByUserId"] = importedByUserId;
                                        newRow["ImportedByUsername"] = importedByUsername;
                                        newRow["ImportedDateTime"] = DateTime.Now;
                                        newRow["ImportStatus"] = "Inserted";
                                        newRow["ImportFileName"] = fileName;
                                        newRow["ImportBatchId"] = importBatchId;

                                        dtBatch.Rows.Add(newRow);
                                        totalRowsProcessed++;

                                        if (dtBatch.Rows.Count >= BulkBatchSize)
                                        {
                                            // Pass the active connection and transaction downward
                                            BulkInsertStaging(dtBatch, conn, transaction);
                                            dtBatch.Clear();
                                        }
                                    }

                                    // Flush remaining rows
                                    if (dtBatch.Rows.Count > 0)
                                    {
                                        BulkInsertStaging(dtBatch, conn, transaction);
                                        dtBatch.Clear();
                                    }

                                    // ONLY COMMIT if every single batch was inserted flawlessly
                                    transaction.Commit();
                                }
                                catch
                                {
                                    // If ANY row in ANY batch fails, rollback the ENTIRE file import. No orphaned data!
                                    transaction.Rollback();
                                    throw;
                                }
                            }
                        }
                    }
                }

                // 3. Process Data to Final Table (Only executes if Staging was 100% successful)
                ProcessStagingToFinal(importBatchId);

                // 4. Bind Grids
                BindPreviewGrid(importBatchId);
                BindErrorGrid(importBatchId);
            }
            catch (Exception ex)
            {
                lblMessage.Text = "A critical error occurred during import. The upload was safely aborted. Error: " + ex.Message;
                lblMessage.CssClass = "text-danger font-weight-bold";
            }
        }

        // Updated Method Signature: Uses the parent's Connection and Transaction
        private void BulkInsertStaging(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                bulk.DestinationTableName = "dbo.SalesDailyActivity_Staging";
                // Let SqlBulkCopy optimize network packets based on our DataTable size
                bulk.BatchSize = dt.Rows.Count;
                bulk.BulkCopyTimeout = 600;

                foreach (DataColumn col in dt.Columns)
                {
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                bulk.WriteToServer(dt);
            }
        }

        private DataTable CreateStagingDataTableSchema()
        {
            var dt = new DataTable();
            // Matching the exact schema of [dbo].[SalesDailyActivity_Staging]
            dt.Columns.Add("RawJson", typeof(string));
            dt.Columns.Add("Zone", typeof(string));
            dt.Columns.Add("Reporting_Manager3", typeof(string));
            dt.Columns.Add("Reporting_Manager2", typeof(string));
            dt.Columns.Add("Reporting_Manager1", typeof(string));
            dt.Columns.Add("Login_Id", typeof(string));
            dt.Columns.Add("User_Id", typeof(string));
            dt.Columns.Add("User_Name", typeof(string));
            dt.Columns.Add("Month", typeof(string));
            dt.Columns.Add("Attendance_Status", typeof(string));
            dt.Columns.Add("Total_Distance_Travelled_In_KM", typeof(decimal));
            dt.Columns.Add("ImportedByUserId", typeof(string));
            dt.Columns.Add("ImportedByUsername", typeof(string));
            dt.Columns.Add("ImportedDateTime", typeof(DateTime));
            dt.Columns.Add("ImportStatus", typeof(string));
            dt.Columns.Add("ProcessingMessage", typeof(string));
            dt.Columns.Add("ImportFileName", typeof(string));
            dt.Columns.Add("ImportBatchId", typeof(Guid));
            dt.Columns.Add("Date", typeof(DateTime));
            dt.Columns.Add("Day", typeof(string));
            return dt;
        }

        private void BulkInsertStaging(DataTable dt)
        {
            using (var conn = new SqlConnection(_connString))
            {
                conn.Open();
                // Wrap bulk copy in a transaction to ensure no partial batch loads
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulk.DestinationTableName = "dbo.SalesDailyActivity_Staging";
                            bulk.BatchSize = dt.Rows.Count;
                            bulk.BulkCopyTimeout = 600;

                            // Map columns dynamically based on DataTable schema to prevent ordinal mismatch
                            foreach (DataColumn col in dt.Columns)
                            {
                                bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            }

                            bulk.WriteToServer(dt);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw; // Re-throw to be caught in btnUpload_Click
                    }
                }
            }
        }

        private void ProcessStagingToFinal(Guid importBatchId)
        {
            using (var conn = new SqlConnection(_connString))
            {
                using (var cmd = new SqlCommand("dbo.Process_StagingToFinal_FromJson_Idempotent", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600; // 10 minutes for large datasets
                    cmd.Parameters.AddWithValue("@ImportBatchId", importBatchId);

                    var userParam = cmd.Parameters.AddWithValue("@ProcessedByUser", HttpContext.Current.User.Identity.Name ?? "system");
                    userParam.IsNullable = true;

                    conn.Open();

                    // Read the summary output from your Stored Procedure
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int insertedRows = reader["InsertedRows"] != DBNull.Value ? Convert.ToInt32(reader["InsertedRows"]) : 0;
                            int updatedRows = reader["UpdatedRows"] != DBNull.Value ? Convert.ToInt32(reader["UpdatedRows"]) : 0;
                            int errorRows = reader["ErrorRows"] != DBNull.Value ? Convert.ToInt32(reader["ErrorRows"]) : 0;

                            string resultMsg = $"<strong>Import Complete!</strong> Successfully Inserted: {insertedRows} | Updated: {updatedRows}.";

                            if (errorRows > 0)
                            {
                                resultMsg += $" <br/><span class='text-danger'>Warning: {errorRows} rows failed validation (e.g., missing User_Id or Date). Please check the Error Logs.</span>";
                                lblMessage.CssClass = "text-warning";
                            }
                            else
                            {
                                lblMessage.CssClass = "text-success";
                            }

                            lblMessage.Text = resultMsg;
                        }
                        else
                        {
                            lblMessage.Text = "Import completed, but no summary was returned from the database.";
                            lblMessage.CssClass = "text-info";
                        }
                    }
                }
            }
        }

        private void BindPreviewGrid(Guid importBatchId)
        {
            // Select TOP 50 to prevent memory crashes on the UI when viewing 31,000 rows
            using (var conn = new SqlConnection(_connString))
            {
                string query = @"SELECT TOP 50 Zone, User_Id, User_Name, Month, [Day], [Date], Attendance_Status, Total_Distance_Travelled_In_KM 
                                 FROM dbo.SalesDailyActivity_Staging 
                                 WHERE ImportBatchId = @BatchId 
                                 ORDER BY StagingId ASC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BatchId", importBatchId);
                    var da = new SqlDataAdapter(cmd);
                    var dtPreview = new DataTable();
                    da.Fill(dtPreview);

                    gvPreview.DataSource = dtPreview;
                    gvPreview.DataBind();
                }
            }
        }

        private string SafeGet(IDictionary<string, string> dict, string key)
        {
            if (dict == null) return null;
            string val;
            if (dict.TryGetValue(key, out val) && !string.IsNullOrWhiteSpace(val))
            {
                return CleanString(val);
            }
            return null;
        }

        private string CleanString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            s = s.Replace("\uFEFF", ""); // Remove Byte Order Mark
            s = s.Replace("\u00A0", " "); // Replace Non-breaking space
            return s;
        }

        private void BindErrorGrid(Guid importBatchId)
        {
            using (var conn = new SqlConnection(_connString))
            {
                // Query the error log table using the current Batch ID
                string query = @"SELECT StagingId, ErrorStage, ErrorReason, RawJson 
                         FROM dbo.ImportRowErrorLog 
                         WHERE ImportBatchId = @BatchId 
                         ORDER BY StagingId ASC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BatchId", importBatchId);
                    var da = new SqlDataAdapter(cmd);
                    var dtErrors = new DataTable();
                    da.Fill(dtErrors);

                    // If we have errors, bind the grid and show the div
                    if (dtErrors.Rows.Count > 0)
                    {
                        gvErrors.DataSource = dtErrors;
                        gvErrors.DataBind();
                        divErrorLog.Visible = true; // Reveal the error panel to the user
                    }
                    else
                    {
                        // Safety catch: ensure it's hidden if there are no errors
                        divErrorLog.Visible = false;
                    }
                }
            }
        }
    }
}