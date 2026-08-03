using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Web.UI.WebControls;
using Microsoft.VisualBasic.FileIO;
using System.Linq;

namespace AnmolDristi
{
    public partial class HrmsDataImport : BasePage
    {
        // Define the exact columns your DB Staging Table expects
        private readonly string[] ExpectedDbColumns = new string[]
        {
            // Core
            "EmpCode", "EmpName", "EmpStatus", "EmployeeStatus", "Email", "MobileNo",
            "DOJ", "Gender", 
            // Master IDs & Descriptions
            "Branch_ID", "Branch_Description",
            "Department_ID", "Department_Description",
            "Designation_ID", "Designation_Description",
            "Grade_ID", "Grade_Description",
            "PlantID_ID", "PlantID_Description",
            "Region_ID", "Region_Description",
            "Division_ID", "Division_Description",
            "Category_ID", "Category_Description",
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
            if (string.IsNullOrWhiteSpace(CurrentEmployeeCode))
            {
                Response.Redirect("~/Login.aspx?msg=session_expired", true);
                return;
            }
        }

        protected void btnReadHeaders_Click(object sender, EventArgs e)
        {
            if (!fileUploadCsv.HasFile)
            {
                ShowMessage("Please select a file to upload.", false);
                return;
            }

            string fileExtension = Path.GetExtension(fileUploadCsv.FileName).ToLowerInvariant();

            if (fileExtension != ".csv")
            {
                ShowMessage("Invalid file type. Only .csv files are allowed.", false);
                return;
            }

            int maxFileSize = 20 * 1024 * 1024; //20 MB

            if (fileUploadCsv.PostedFile.ContentLength > maxFileSize)
            {
                ShowMessage("File is too large. Maximum allowed size is 20MB.", false);
                return;
            }

            try
            {
                string tempFolder = Server.MapPath("~/TempUploads/");

                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                string uniqueFileName =
                    string.Format("{0}_{1}",
                    Guid.NewGuid().ToString("N"),
                    fileUploadCsv.FileName);

                string fullPath = Path.Combine(tempFolder, uniqueFileName);

                fileUploadCsv.SaveAs(fullPath);

                ViewState["TempCsvPath"] = fullPath;
                ViewState["OriginalFileName"] = fileUploadCsv.FileName;

                List<string> excelHeaders = new List<string>();

                using (StreamReader sr = new StreamReader(fullPath))
                using (TextFieldParser parser = new TextFieldParser(sr))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    if (parser.EndOfData)
                    {
                        CleanupTempUpload(fullPath);
                        ShowMessage("The uploaded file is empty.", false);
                        return;
                    }

                    string[] headers = parser.ReadFields();

                    if (headers == null || headers.Length == 0)
                    {
                        CleanupTempUpload(fullPath);
                        ShowMessage("CSV header row is missing or unreadable.", false);
                        return;
                    }

                    foreach (string h in headers)
                    {
                        if (string.IsNullOrWhiteSpace(h))
                            continue;

                        string cleanHeader = h
                            .Trim()
                            .Trim('\uFEFF')
                            .Replace("\"", "");

                        excelHeaders.Add(cleanHeader);
                    }

                    if (excelHeaders.Count == 0)
                    {
                        CleanupTempUpload(fullPath);
                        ShowMessage("CSV header row does not contain any valid column names.", false);
                        return;
                    }
                }

                ViewState["ExcelHeaders"] = excelHeaders;

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
                ShowMessage("Error reading file: " + ex.Message, false);
            }
        }

        protected void rptMapping_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            HiddenField hfDbColumn =
                (HiddenField)e.Item.FindControl("hfDbColumn");

            DropDownList ddlExcelColumn =
                (DropDownList)e.Item.FindControl("ddlExcelColumn");

            List<string> excelHeaders =
                ViewState["ExcelHeaders"] as List<string>;

            ddlExcelColumn.Items.Clear();

            ddlExcelColumn.Items.Add(
                new ListItem("-- Ignore / Do Not Map --", ""));

            if (excelHeaders == null || excelHeaders.Count == 0)
                return;

            foreach (string header in excelHeaders)
            {
                ddlExcelColumn.Items.Add(
                    new ListItem(header, header));
            }

            string dbColumn = hfDbColumn.Value
                .Trim()
                .Trim('\uFEFF')
                .Replace("\"", "");

            foreach (ListItem item in ddlExcelColumn.Items)
            {
                string excelColumn = item.Text
                    .Trim()
                    .Trim('\uFEFF')
                    .Replace("\"", "");

                if (string.Equals(
                        excelColumn,
                        dbColumn,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ddlExcelColumn.SelectedValue = item.Value;
                    break;
                }
            }
        }

        protected void btnProcess_Click(object sender, EventArgs e)
        {
            string tempPath = ViewState["TempCsvPath"] as string;

            if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
            {
                ShowMessage("File reference lost. Please upload the file again.", false);
                btnCancel_Click(null, null);
                return;
            }

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

            string templateError;
            if (!ValidateTemplateMapping(dbToExcelMap, out templateError))
            {
                ShowMessage(templateError, false);
                return;
            }

            string connectionString = GetConnectionString();
            int batchId = 0;
            bool uploadSucceeded = false;

            try
            {
                using (StreamReader sr = new StreamReader(tempPath))
                using (TextFieldParser parser = new TextFieldParser(sr))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    Dictionary<string, int> headerIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (parser.EndOfData)
                    {
                        ShowMessage("The uploaded file is empty.", false);
                        return;
                    }

                    string[] headers = parser.ReadFields();
                    if (headers == null || headers.Length == 0)
                    {
                        ShowMessage("CSV header row is missing or unreadable.", false);
                        return;
                    }

                    for (int i = 0; i < headers.Length; i++)
                    {
                        string headerName = headers[i]
                            .Trim()
                            .Trim('\uFEFF')      // Remove UTF-8 BOM
                            .Replace("\"", "");  // Remove quotes if present

                        if (!headerIndexMap.ContainsKey(headerName))
                        {
                            headerIndexMap.Add(headerName, i);
                        }
                    }

                    if (!ValidateMappedHeadersExist(dbToExcelMap, headerIndexMap, out templateError))
                    {
                        ShowMessage(templateError, false);
                        return;
                    }

                    if (parser.EndOfData)
                    {
                        ShowMessage("The file contains a header row but no data rows.", false);
                        return;
                    }

                    // 2. Create Batch ID with Status = Uploaded
                    string uploadedBy = Session["UserId"] != null ? Session["UserId"].ToString() : "UnknownSystemUser";
                    string originalFileName = ViewState["OriginalFileName"] != null ? ViewState["OriginalFileName"].ToString() : "UnknownFile.csv";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        string insertBatchSql =
                            "INSERT INTO dbo.ImportBatchMaster (FileName, UploadedBy, Status) " +
                            "OUTPUT INSERTED.BatchId VALUES (@FileName, @UploadedBy, @Status)";
                        using (SqlCommand cmd = new SqlCommand(insertBatchSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@FileName", originalFileName);
                            cmd.Parameters.AddWithValue("@UploadedBy", uploadedBy);
                            cmd.Parameters.AddWithValue("@Status", "Uploaded");
                            conn.Open();
                            batchId = (int)cmd.ExecuteScalar();
                        }
                    }

                    // 3. Read CSV and Apply Dynamic User Mapping → Staging_EmployeeData
                    DataTable dtStaging = CreateStagingTable();

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        DataRow row = dtStaging.NewRow();
                        row["BatchId"] = batchId;

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
                uploadSucceeded = true;

                ViewState.Remove("TempCsvPath");
                ViewState.Remove("ExcelHeaders");
                ViewState.Remove("OriginalFileName");
                //ProcessBatch(batchId, chkAllowOverwrite.Checked, connectionString);

                //btnCancel_Click(null, null);
            }
            catch (Exception ex)
            {
                // Best-effort cleanup of orphan data
                if (batchId > 0)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();

                            using (SqlTransaction tx = conn.BeginTransaction())
                            {
                                try
                                {
                                    using (SqlCommand cmd = new SqlCommand(@"
                            DELETE FROM dbo.Staging_EmployeeData
                            WHERE BatchId = @BatchId;

                            DELETE FROM dbo.ImportBatchMaster
                            WHERE BatchId = @BatchId;",
                                        conn, tx))
                                    {
                                        cmd.Parameters.AddWithValue("@BatchId", batchId);
                                        cmd.ExecuteNonQuery();
                                    }

                                    tx.Commit();
                                }
                                catch
                                {
                                    if (tx.Connection != null)
                                        tx.Rollback();

                                    // Do not throw - preserve the original exception
                                }
                            }
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        // Log only. Never replace the original upload exception.
                        System.Diagnostics.Debug.WriteLine(
                            "Batch cleanup failed: " + cleanupEx.Message);
                    }
                }

                ShowMessage(
                    string.Format("A critical error occurred: {0}", ex.Message),
                    false);
            }
            finally
            {
                // FIX: Safe cleanup without forcing garbage collection (GC.Collect removed)
                try
                {
                    if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to delete temp file: " + cleanupEx.Message);
                }
            }

            if (uploadSucceeded && batchId > 0)
            {
                Response.Redirect(
                    string.Format("HrmsBatchManager.aspx?BatchId={0}", batchId),
                    false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        private bool ValidateTemplateMapping(Dictionary<string, string> dbToExcelMap, out string errorMessage)
        {
            errorMessage = null;

            if (dbToExcelMap == null || dbToExcelMap.Count == 0)
            {
                errorMessage = "Please map at least EmpCode before uploading to staging.";
                return false;
            }

            if (!dbToExcelMap.ContainsKey("EmpCode") || string.IsNullOrWhiteSpace(dbToExcelMap["EmpCode"]))
            {
                errorMessage = "Template validation failed: EmpCode must be mapped to a source column.";
                return false;
            }

            return true;
        }

        private bool ValidateMappedHeadersExist(
            Dictionary<string, string> dbToExcelMap,
            Dictionary<string, int> headerIndexMap,
            out string errorMessage)
        {
            errorMessage = null;

            foreach (KeyValuePair<string, string> mapping in dbToExcelMap)
            {
                if (!headerIndexMap.ContainsKey(mapping.Value))
                {
                    errorMessage = string.Format(
                        "Template validation failed: mapped source column '{0}' was not found in the file header.",
                        mapping.Value);
                    return false;
                }
            }

            return true;
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlUpload.Visible = true;
            pnlMapping.Visible = false;
            lblMessage.Text = string.Empty;

            ViewState.Remove("TempCsvPath");
            ViewState.Remove("ExcelHeaders");
            ViewState.Remove("OriginalFileName");
        }

        private void CleanupTempUpload(string fullPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception cleanupEx)
            {
                System.Diagnostics.Debug.WriteLine("Failed to delete temp file: " + cleanupEx.Message);
            }
            // Clear ViewState properly
            ViewState.Remove("TempCsvPath");
            ViewState.Remove("ExcelHeaders");
            ViewState.Remove("OriginalFileName");
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
            if (dt == null || dt.Rows.Count == 0)
                return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(
                            conn,
                            SqlBulkCopyOptions.Default,
                            transaction))
                        {
                            bulkCopy.DestinationTableName = "dbo.Staging_EmployeeData";
                            bulkCopy.BulkCopyTimeout = 300;
                            bulkCopy.BatchSize = 5000;

                            foreach (DataColumn col in dt.Columns)
                            {
                                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            }

                            bulkCopy.WriteToServer(dt);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        if (transaction.Connection != null)
                        {
                            transaction.Rollback();
                        }

                        throw;
                    }
                }
            }
        }

        private void ShowMessage(string message, bool isSuccess)
        {
            lblMessage.Text = message;
            lblMessage.CssClass = isSuccess ? "font-weight-bold d-block mb-3 text-success" : "font-weight-bold d-block mb-3 text-danger";
        }
            
    }
}
