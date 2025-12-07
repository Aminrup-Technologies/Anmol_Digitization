using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Microsoft.VisualBasic.FileIO;

namespace AnmolDristi
{
    public partial class ImportCsv : AnmolDristi.BasePage
    {
        private readonly string _connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        // Bulk batch size (tune per memory/throughput)
        private const int BulkBatchSize = 2000;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // At this point session is already validated by BasePage
                //lblUser.Text = CurrentEmployeeCode;   // safe to use
            }
        }

        //protected void btnUpload_Click(object sender, EventArgs e)
        //{
        //    if (!fuCsv.HasFile)
        //    {
        //        lblMessage.Text = "Please choose a CSV file.";
        //        return;
        //    }

        //    var importBatchId = Guid.NewGuid();
        //    var fileName = fuCsv.FileName;
        //    var importedByUserId = HttpContext.Current.User.Identity.Name ?? "system";
        //    var importedByUsername = HttpContext.Current.User.Identity.Name ?? "system";

        //    try
        //    {
        //        using (var stream = fuCsv.PostedFile.InputStream)
        //        using (var reader = new StreamReader(stream))
        //        {
        //            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        //            {
        //                HasHeaderRecord = true,
        //                IgnoreBlankLines = true,
        //                TrimOptions = TrimOptions.Trim,
        //                BadDataFound = null
        //            };

        //            using (var csv = new CsvReader(reader, csvConfig))
        //            {
        //                // Read header
        //                if (!csv.Read() || !csv.ReadHeader())
        //                {
        //                    lblMessage.Text = "CSV has no header or is empty.";
        //                    return;
        //                }

        //                var headers = csv.Context.HeaderRecord; // string[]

        //                var dtBatch = CreateStagingDataTableSchema();
        //                int rowCount = 0;

        //                while (csv.Read())
        //                {
        //                    // Build dictionary of header -> value for this row
        //                    var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //                    foreach (var h in headers)
        //                    {
        //                        string value = null;
        //                        try
        //                        {
        //                            value = csv.GetField(h);
        //                        }
        //                        catch
        //                        {
        //                            // If header lookup fails, try to skip or set empty
        //                            value = string.Empty;
        //                        }
        //                        normalized[h?.Trim() ?? ""] = value ?? string.Empty;
        //                    }

        //                    // Serialize the whole row to RawJson
        //                    var rawJson = JsonConvert.SerializeObject(normalized);

        //                    var newRow = dtBatch.NewRow();
        //                    newRow["RawJson"] = rawJson;

        //                    // Typed columns
        //                    newRow["Zone"] = SafeGet(normalized, "Zone");
        //                    newRow["Reporting_Manager3"] = SafeGet(normalized, "Reporting_Manager3");
        //                    newRow["Reporting_Manager2"] = SafeGet(normalized, "Reporting_Manager2");
        //                    newRow["Reporting_Manager1"] = SafeGet(normalized, "Reporting_Manager1");
        //                    newRow["Login_Id"] = SafeGet(normalized, "Login_Id");
        //                    newRow["User_Id"] = SafeGet(normalized, "User_Id");
        //                    newRow["User_Name"] = SafeGet(normalized, "User_Name");
        //                    newRow["Month"] = SafeGet(normalized, "Month");
        //                    newRow["Attendance_Status"] = SafeGet(normalized, "Attendance_Status");

        //                    // Total distance parse with fallbacks
        //                    var kmStr = SafeGet(normalized, "Total_Distance_Travelled_In_KM");
        //                    if (string.IsNullOrWhiteSpace(kmStr))
        //                    {
        //                        kmStr = SafeGet(normalized, "TotalDistanceTravelledInKM");
        //                        if (string.IsNullOrWhiteSpace(kmStr))
        //                            kmStr = SafeGet(normalized, "Total Distance Travelled In KM"); // try spaced variant
        //                    }

        //                    decimal kmVal;
        //                    if (decimal.TryParse(kmStr, NumberStyles.Any, CultureInfo.InvariantCulture, out kmVal))
        //                        newRow["Total_Distance_Travelled_In_KM"] = kmVal;
        //                    else
        //                        newRow["Total_Distance_Travelled_In_KM"] = DBNull.Value;

        //                    // Metadata
        //                    newRow["ImportedByUserId"] = importedByUserId;
        //                    newRow["ImportedByUsername"] = importedByUsername;
        //                    newRow["ImportedDateTime"] = DateTime.UtcNow;
        //                    newRow["ImportStatus"] = "Inserted";
        //                    newRow["ProcessingMessage"] = DBNull.Value;
        //                    newRow["ImportFileName"] = fileName;
        //                    newRow["ImportBatchId"] = importBatchId;

        //                    dtBatch.Rows.Add(newRow);
        //                    rowCount++;

        //                    if (dtBatch.Rows.Count >= BulkBatchSize)
        //                    {
        //                        BulkInsertStaging(dtBatch);
        //                        dtBatch.Clear();
        //                    }
        //                } // while csv.Read()

        //                if (dtBatch.Rows.Count > 0)
        //                {
        //                    BulkInsertStaging(dtBatch);
        //                    dtBatch.Clear();
        //                }

        //                lblMessage.Text = $"Import completed. Rows processed: {rowCount}. BatchId: {importBatchId}";
        //            } // using csv
        //        } // using reader/stream

        //        // Optionally call normalization proc here:
        //        // ProcessStagingToFinal();

        //    }
        //    catch (Exception ex)
        //    {
        //        lblMessage.Text = "Import failed: " + ex.Message;
        //    }
        //}

        protected void btnUpload_Click_OLD2(object sender, EventArgs e)
        {
            if (!fuCsv.HasFile)
            {
                lblMessage.Text = "Please choose a CSV file.";
                return;
            }

            var importBatchId = Guid.NewGuid();
            var fileName = fuCsv.FileName;
            var importedByUserId = HttpContext.Current.User.Identity.Name ?? "system";
            var importedByUsername = HttpContext.Current.User.Identity.Name ?? "system";

            try
            {
                using (var stream = fuCsv.PostedFile.InputStream)
                {
                    // Ensure stream start
                    if (stream.CanSeek)
                        stream.Position = 0;

                    // TextFieldParser reads CSV reliably (handles quoted fields, embedded commas)
                    using (var tfp = new TextFieldParser(stream))
                    {
                        tfp.TextFieldType = FieldType.Delimited;
                        tfp.SetDelimiters(",");
                        tfp.HasFieldsEnclosedInQuotes = true;

                        // Read header
                        if (tfp.EndOfData)
                        {
                            lblMessage.Text = "CSV is empty.";
                            return;
                        }

                        string[] headers = null;
                        try
                        {
                            headers = tfp.ReadFields(); // header row
                        }
                        catch (MalformedLineException mex)
                        {
                            lblMessage.Text = "CSV header malformed: " + mex.Message;
                            return;
                        }

                        if (headers == null || headers.Length == 0)
                        {
                            lblMessage.Text = "CSV header is missing.";
                            return;
                        }

                        // Trim headers
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (headers[i] != null)
                                headers[i] = headers[i].Trim();
                            else
                                headers[i] = string.Empty;
                        }

                        var dtBatch = CreateStagingDataTableSchema();
                        int rowCount = 0;

                        while (!tfp.EndOfData)
                        {
                            string[] fields = null;
                            try
                            {
                                fields = tfp.ReadFields();
                            }
                            catch (MalformedLineException)
                            {
                                // skip malformed line; you can instead record an error row into staging
                                continue;
                            }

                            // Build dictionary header->value
                            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                string key = headers[i];
                                string val = string.Empty;
                                if (fields != null && i < fields.Length && fields[i] != null)
                                    val = fields[i];
                                normalized[key] = val;
                            }

                            // Serialize whole row to RawJson
                            var rawJson = JsonConvert.SerializeObject(normalized);

                            var newRow = dtBatch.NewRow();
                            newRow["RawJson"] = rawJson;

                            // Typed columns
                            newRow["Zone"] = SafeGet(normalized, "Zone");
                            newRow["Reporting_Manager3"] = SafeGet(normalized, "Reporting_Manager3");
                            newRow["Reporting_Manager2"] = SafeGet(normalized, "Reporting_Manager2");
                            newRow["Reporting_Manager1"] = SafeGet(normalized, "Reporting_Manager1");
                            newRow["Login_Id"] = SafeGet(normalized, "Login_Id");
                            newRow["User_Id"] = SafeGet(normalized, "User_Id");
                            newRow["User_Name"] = SafeGet(normalized, "User_Name");
                            newRow["Month"] = SafeGet(normalized, "Month");
                            newRow["Attendance_Status"] = SafeGet(normalized, "Attendance_Status");

                            // Total distance parse with fallbacks
                            string kmStr = SafeGet(normalized, "Total_Distance_Travelled_In_KM");
                            if (string.IsNullOrWhiteSpace(kmStr))
                            {
                                kmStr = SafeGet(normalized, "TotalDistanceTravelledInKM");
                                if (string.IsNullOrWhiteSpace(kmStr))
                                    kmStr = SafeGet(normalized, "Total Distance Travelled In KM");
                            }

                            decimal kmVal;
                            if (decimal.TryParse(kmStr, NumberStyles.Any, CultureInfo.InvariantCulture, out kmVal))
                                newRow["Total_Distance_Travelled_In_KM"] = kmVal;
                            else
                                newRow["Total_Distance_Travelled_In_KM"] = DBNull.Value;

                            // Metadata
                            newRow["ImportedByUserId"] = importedByUserId;
                            newRow["ImportedByUsername"] = importedByUsername;
                            newRow["ImportedDateTime"] = DateTime.UtcNow;
                            newRow["ImportStatus"] = "Inserted";
                            newRow["ProcessingMessage"] = DBNull.Value;
                            newRow["ImportFileName"] = fileName;
                            newRow["ImportBatchId"] = importBatchId;

                            dtBatch.Rows.Add(newRow);
                            rowCount++;

                            if (dtBatch.Rows.Count >= BulkBatchSize)
                            {
                                BulkInsertStaging(dtBatch);
                                dtBatch.Clear();
                            }
                        } // end while

                        // flush remainder
                        if (dtBatch.Rows.Count > 0)
                        {
                            BulkInsertStaging(dtBatch);
                            dtBatch.Clear();
                        }

                        lblMessage.Text = "Import completed. Rows processed: " + rowCount.ToString() + ". BatchId: " + importBatchId.ToString();
                    } // using tfp
                } // using stream

                // Optionally call normalization stored proc
                // ProcessStagingToFinal();
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Import failed: " + ex.Message;
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (!fuCsv.HasFile)
            {
                lblMessage.Text = "Please choose a CSV file.";
                return;
            }

            var importBatchId = Guid.NewGuid();
            var fileName = fuCsv.FileName;
            var importedByUserId = HttpContext.Current.User.Identity.Name ?? "system";
            var importedByUsername = HttpContext.Current.User.Identity.Name ?? "system";

            try
            {
                using (var stream = fuCsv.PostedFile.InputStream)
                {
                    // Ensure stream start
                    if (stream.CanSeek)
                        stream.Position = 0;

                    using (var tfp = new Microsoft.VisualBasic.FileIO.TextFieldParser(stream))
                    {
                        tfp.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                        tfp.SetDelimiters(",");
                        tfp.HasFieldsEnclosedInQuotes = true;

                        // Read header
                        if (tfp.EndOfData)
                        {
                            lblMessage.Text = "CSV is empty.";
                            return;
                        }

                        string[] headers = null;
                        try
                        {
                            headers = tfp.ReadFields(); // header row
                        }
                        catch (Microsoft.VisualBasic.FileIO.MalformedLineException mex)
                        {
                            lblMessage.Text = "CSV header malformed: " + mex.Message;
                            return;
                        }

                        if (headers == null || headers.Length == 0)
                        {
                            lblMessage.Text = "CSV header is missing.";
                            return;
                        }

                        // Trim headers
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (headers[i] != null)
                                headers[i] = headers[i].Trim();
                            else
                                headers[i] = string.Empty;
                        }

                        var dtBatch = CreateStagingDataTableSchema();
                        int rowCount = 0;

                        while (!tfp.EndOfData)
                        {
                            string[] fields = null;
                            try
                            {
                                fields = tfp.ReadFields();
                            }
                            catch (Microsoft.VisualBasic.FileIO.MalformedLineException)
                            {
                                // skip malformed line; optionally record an error row into staging
                                continue;
                            }

                            // Build dictionary header->value
                            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                string key = headers[i];
                                string val = string.Empty;
                                if (fields != null && i < fields.Length && fields[i] != null)
                                    val = fields[i];
                                normalized[key] = val;
                            }

                            // Serialize whole row to RawJson
                            var rawJson = Newtonsoft.Json.JsonConvert.SerializeObject(normalized);

                            var newRow = dtBatch.NewRow();
                            newRow["RawJson"] = rawJson;

                            // Typed columns
                            newRow["Zone"] = SafeGet(normalized, "Zone");
                            newRow["Reporting_Manager3"] = SafeGet(normalized, "Reporting_Manager3");
                            newRow["Reporting_Manager2"] = SafeGet(normalized, "Reporting_Manager2");
                            newRow["Reporting_Manager1"] = SafeGet(normalized, "Reporting_Manager1");
                            newRow["Login_Id"] = SafeGet(normalized, "Login_Id");
                            newRow["User_Id"] = SafeGet(normalized, "User_Id");
                            newRow["User_Name"] = SafeGet(normalized, "User_Name");
                            newRow["Month"] = SafeGet(normalized, "Month");

                            // --- Day/Date parsing (robust, prioritizes dd-MM-yyyy) ---
                            string dayStr = SafeGet(normalized, "Day");
                            string dateStr = SafeGet(normalized, "Date");

                            // alternate headers
                            if (string.IsNullOrWhiteSpace(dayStr)) dayStr = SafeGet(normalized, "Weekday");
                            if (string.IsNullOrWhiteSpace(dateStr)) dateStr = SafeGet(normalized, "User_Created_Date");
                            if (string.IsNullOrWhiteSpace(dateStr)) dateStr = SafeGet(normalized, "ActivityDate");

                            // final fallback: any header containing "date"
                            if (string.IsNullOrWhiteSpace(dateStr))
                            {
                                // SafeGet already tries contains(key) but be explicit:
                                foreach (var k in normalized.Keys)
                                {
                                    if (!string.IsNullOrWhiteSpace(k) && k.IndexOf("date", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        var candidate = normalized[k];
                                        if (!string.IsNullOrWhiteSpace(candidate))
                                        {
                                            dateStr = CleanString(candidate);
                                            break;
                                        }
                                    }
                                }
                            }

                            DateTime parsedDate = DateTime.MinValue;
                            bool haveDate = false;

                            if (!string.IsNullOrWhiteSpace(dateStr))
                            {
                                var cleaned = NormalizeDateString(dateStr);

                                // list of common formats seen in CSV exports; keep ISO and dd-MM first (IST expected)
                                string[] formats = new[] {
                                    "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
                                    "yyyy-MM-dd", "yyyyMMdd", "MM/dd/yyyy", "M/d/yyyy",
                                    "dd-MM-yy", "dd/MM/yy"
                                };

                                // Try exact formats using en-GB (favours day-month-year)
                                var enGb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
                                if (!string.IsNullOrWhiteSpace(cleaned))
                                {
                                    if (DateTime.TryParseExact(cleaned, formats, enGb, System.Globalization.DateTimeStyles.None, out parsedDate)
                                        || DateTime.TryParseExact(cleaned, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                                    {
                                        haveDate = true;
                                    }
                                    else if (DateTime.TryParse(cleaned, enGb, System.Globalization.DateTimeStyles.None, out parsedDate))
                                    {
                                        haveDate = true;
                                    }
                                    else if (DateTime.TryParse(cleaned, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                                    {
                                        haveDate = true;
                                    }
                                }

                                if (!haveDate)
                                {
                                    // record debug info for this row so you can see exact failing content
                                    string existingMsg = string.Empty;
                                    if (newRow.Table.Columns.Contains("ProcessingMessage") && newRow["ProcessingMessage"] != null && newRow["ProcessingMessage"] != DBNull.Value)
                                        existingMsg = newRow["ProcessingMessage"].ToString();

                                    string msg = "Missing/Unparseable Date (raw: \"" + (dateStr ?? "<null>") + "\").";
                                    newRow["ProcessingMessage"] = (existingMsg + " " + msg).Trim();
                                }
                            }

                            if (haveDate)
                            {
                                // store date (only date part) and canonical day
                                newRow["Date"] = parsedDate.Date;
                                newRow["Day"] = parsedDate.DayOfWeek.ToString();

                                if (!string.IsNullOrWhiteSpace(dayStr) && !string.Equals(dayStr.Trim(), parsedDate.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    string existingMsg = string.Empty;
                                    if (newRow.Table.Columns.Contains("ProcessingMessage") && newRow["ProcessingMessage"] != null && newRow["ProcessingMessage"] != DBNull.Value)
                                        existingMsg = newRow["ProcessingMessage"].ToString();

                                    string msg = "Day/Date mismatch (day provided: " + dayStr.Trim() + ", date parsed weekday: " + parsedDate.DayOfWeek.ToString() + ").";
                                    newRow["ProcessingMessage"] = (existingMsg + " " + msg).Trim();
                                }
                            }
                            else
                            {
                                // keep Date as DBNull and set Day if provided
                                newRow["Date"] = DBNull.Value;
                                if (!string.IsNullOrWhiteSpace(dayStr))
                                {
                                    try { newRow["Day"] = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(dayStr.Trim().ToLower()); }
                                    catch { newRow["Day"] = dayStr.Trim(); }
                                }
                                else
                                {
                                    newRow["Day"] = DBNull.Value;
                                }
                            }


                            //----------Commented on 07-Dec-225--------------------------- //
                            //// --- Day/Date parsing (robust, prioritizes dd-MM-yyyy) ---
                            //string dayStr = SafeGet(normalized, "Day");
                            //string dateStr = SafeGet(normalized, "Date");

                            //// alternate headers
                            //if (string.IsNullOrWhiteSpace(dayStr)) dayStr = SafeGet(normalized, "Weekday");
                            //if (string.IsNullOrWhiteSpace(dateStr)) dateStr = SafeGet(normalized, "User_Created_Date");
                            //if (string.IsNullOrWhiteSpace(dateStr)) dateStr = SafeGet(normalized, "ActivityDate");

                            //// attempt combined-field detection (unchanged from previous logic)...
                            //// (you can keep your existing scan-through-normalized block here if needed)

                            //// New parsing: prefer exact dd-MM-yyyy formats
                            //System.DateTime parsedDate = System.DateTime.MinValue;
                            //bool haveDate = false;
                            //if (!string.IsNullOrWhiteSpace(dateStr))
                            //{
                            //    string[] formats = new[] {
                            //        "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
                            //        "MM/dd/yyyy", "yyyy-MM-dd", "yyyyMMdd"
                            //    };

                            //    // Try exact parse with invariant (explicit formats)
                            //    if (System.DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                            //    {
                            //        haveDate = true;
                            //    }
                            //    else
                            //    {
                            //        // Try with English (United Kingdom) culture which uses dd-MM-yyyy
                            //        try
                            //        {
                            //            var enGb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
                            //            if (System.DateTime.TryParseExact(dateStr, formats, enGb, System.Globalization.DateTimeStyles.None, out parsedDate))
                            //                haveDate = true;
                            //            else if (System.DateTime.TryParse(dateStr, enGb, System.Globalization.DateTimeStyles.None, out parsedDate))
                            //                haveDate = true;
                            //        }
                            //        catch
                            //        {
                            //            // fall through
                            //        }

                            //        // final fallback: a flexible TryParse with invariant culture
                            //        if (!haveDate)
                            //        {
                            //            if (System.DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                            //                haveDate = true;
                            //        }
                            //    }
                            //}

                            //if (haveDate)
                            //{
                            //    newRow["Date"] = parsedDate;
                            //    // Canonicalize Day from parsed date's weekday
                            //    newRow["Day"] = parsedDate.DayOfWeek.ToString();

                            //    // If CSV provided a Day and it still doesn't match parsed day, append message (optional)
                            //    if (!string.IsNullOrWhiteSpace(dayStr) && !string.Equals(dayStr.Trim(), parsedDate.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase))
                            //    {
                            //        string existingMsg = string.Empty;
                            //        if (newRow.Table.Columns.Contains("ProcessingMessage") && newRow["ProcessingMessage"] != null && newRow["ProcessingMessage"] != DBNull.Value)
                            //            existingMsg = newRow["ProcessingMessage"].ToString();

                            //        string msg = "Day/Date mismatch (day provided: " + dayStr.Trim() + ", date parsed weekday: " + parsedDate.DayOfWeek.ToString() + ").";
                            //        // You may choose to comment this out if you don't want mismatch messages at all.
                            //        newRow["ProcessingMessage"] = (existingMsg + " " + msg).Trim();
                            //    }
                            //}
                            //else
                            //{
                            //    newRow["Date"] = DBNull.Value;
                            //    if (!string.IsNullOrWhiteSpace(dayStr))
                            //    {
                            //        try
                            //        {
                            //            newRow["Day"] = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(dayStr.Trim().ToLower());
                            //        }
                            //        catch
                            //        {
                            //            newRow["Day"] = dayStr.Trim();
                            //        }
                            //    }
                            //    else
                            //    {
                            //        newRow["Day"] = DBNull.Value;
                            //    }
                            //}

                            // --- Total distance: set default 0.00 when missing ---
                            string kmStr = SafeGet(normalized, "Total_Distance_Travelled_In_KM");
                            if (string.IsNullOrWhiteSpace(kmStr))
                            {
                                kmStr = SafeGet(normalized, "TotalDistanceTravelledInKM");
                                if (string.IsNullOrWhiteSpace(kmStr))
                                    kmStr = SafeGet(normalized, "Total Distance Travelled In KM");
                            }

                            decimal kmVal = 0m;
                            if (decimal.TryParse(kmStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out kmVal))
                            {
                                newRow["Total_Distance_Travelled_In_KM"] = kmVal;
                            }
                            else
                            {
                                // default to 0.00
                                newRow["Total_Distance_Travelled_In_KM"] = 0.00m;
                            }

                            newRow["Attendance_Status"] = SafeGet(normalized, "Attendance_Status");

                            //// Total distance parse with fallbacks
                            //string kmStr = SafeGet(normalized, "Total_Distance_Travelled_In_KM");
                            //if (string.IsNullOrWhiteSpace(kmStr))
                            //{
                            //    kmStr = SafeGet(normalized, "TotalDistanceTravelledInKM");
                            //    if (string.IsNullOrWhiteSpace(kmStr))
                            //        kmStr = SafeGet(normalized, "Total Distance Travelled In KM");
                            //}

                            //decimal kmVal;
                            //if (decimal.TryParse(kmStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out kmVal))
                            //    newRow["Total_Distance_Travelled_In_KM"] = kmVal;
                            //else
                            //    newRow["Total_Distance_Travelled_In_KM"] = DBNull.Value;

                            // Metadata
                            newRow["ImportedByUserId"] = importedByUserId;
                            newRow["ImportedByUsername"] = importedByUsername;
                            newRow["ImportedDateTime"] = DateTime.Now;
                            newRow["ImportStatus"] = "Inserted";
                            if (newRow["ProcessingMessage"] == null)
                                newRow["ProcessingMessage"] = DBNull.Value;
                            newRow["ImportFileName"] = fileName;
                            newRow["ImportBatchId"] = importBatchId;

                            dtBatch.Rows.Add(newRow);
                            rowCount++;

                            if (dtBatch.Rows.Count >= BulkBatchSize)
                            {
                                BulkInsertStaging(dtBatch);
                                dtBatch.Clear();
                            }
                        } // end while

                        // flush remainder
                        if (dtBatch.Rows.Count > 0)
                        {
                            BulkInsertStaging(dtBatch);
                            dtBatch.Clear();
                        }


                        // Fetch top 50 rows from this batch and bind to GridView
                        using (var conn = new SqlConnection(_connString))
                        using (var cmd = new SqlCommand(@"SELECT Zone, User_Id, User_Name, Month, [Day], [Date], Attendance_Status, Total_Distance_Travelled_In_KM FROM dbo.SalesDailyActivity_Staging WHERE ImportBatchId = @BatchId ORDER BY StagingId ASC", conn))
                        {
                            cmd.Parameters.AddWithValue("@BatchId", importBatchId);
                            var da = new SqlDataAdapter(cmd);
                            var dtPreview = new DataTable();
                            da.Fill(dtPreview);

                            gvPreview.DataSource = dtPreview;
                            gvPreview.DataBind();
                        }


                        lblMessage.Text = "Import completed. Rows processed: " + rowCount.ToString() + ". BatchId: " + importBatchId.ToString();
                    } // using tfp
                } // using stream

                // Optionally call normalization stored proc here
                // ProcessStagingToFinal();
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Import failed: " + ex.Message;
            }
        }

        private string NormalizeDateString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            s = s.Replace("\"", "");     // stray double quotes
            s = s.Replace("'", "");      // stray single quotes
            s = s.Replace("\uFEFF", ""); // BOM
            s = s.Replace("\u00A0", " "); // non-breaking space
            return s.Trim();
        }



        private DataTable CreateStagingDataTableSchema()
        {
            var dt = new DataTable();

            dt.Columns.Add("RawJson", typeof(string));
            dt.Columns.Add("Zone", typeof(string));
            dt.Columns.Add("Reporting_Manager3", typeof(string));
            dt.Columns.Add("Reporting_Manager2", typeof(string));
            dt.Columns.Add("Reporting_Manager1", typeof(string));
            dt.Columns.Add("Login_Id", typeof(string));
            dt.Columns.Add("User_Id", typeof(string));
            dt.Columns.Add("User_Name", typeof(string));
            dt.Columns.Add("Month", typeof(string));

            // New columns (after Month)
            dt.Columns.Add("Date", typeof(DateTime));   // will be nullable in DB
            dt.Columns.Add("Day", typeof(string));      // weekday name, e.g., Monday

            dt.Columns.Add("Attendance_Status", typeof(string));
            dt.Columns.Add("Total_Distance_Travelled_In_KM", typeof(decimal));

            dt.Columns.Add("ImportedByUserId", typeof(string));
            dt.Columns.Add("ImportedByUsername", typeof(string));
            dt.Columns.Add("ImportedDateTime", typeof(DateTime));
            dt.Columns.Add("ImportStatus", typeof(string));
            dt.Columns.Add("ProcessingMessage", typeof(string));
            dt.Columns.Add("ImportFileName", typeof(string));
            dt.Columns.Add("ImportBatchId", typeof(Guid));

            return dt;
        }

        private void BulkInsertStaging(DataTable dt)
        {
            using (var conn = new SqlConnection(_connString))
            {
                conn.Open();
                using (var bulk = new SqlBulkCopy(conn))
                {
                    bulk.DestinationTableName = "dbo.SalesDailyActivity_Staging";
                    bulk.BatchSize = dt.Rows.Count;

                    bulk.ColumnMappings.Add("RawJson", "RawJson");
                    bulk.ColumnMappings.Add("Zone", "Zone");
                    bulk.ColumnMappings.Add("Reporting_Manager3", "Reporting_Manager3");
                    bulk.ColumnMappings.Add("Reporting_Manager2", "Reporting_Manager2");
                    bulk.ColumnMappings.Add("Reporting_Manager1", "Reporting_Manager1");
                    bulk.ColumnMappings.Add("Login_Id", "Login_Id");
                    bulk.ColumnMappings.Add("User_Id", "User_Id");
                    bulk.ColumnMappings.Add("User_Name", "User_Name");
                    bulk.ColumnMappings.Add("Month", "Month");
                    bulk.ColumnMappings.Add("Date", "Date");
                    bulk.ColumnMappings.Add("Day", "Day");
                    bulk.ColumnMappings.Add("Attendance_Status", "Attendance_Status");
                    bulk.ColumnMappings.Add("Total_Distance_Travelled_In_KM", "Total_Distance_Travelled_In_KM");
                    bulk.ColumnMappings.Add("ImportedByUserId", "ImportedByUserId");
                    bulk.ColumnMappings.Add("ImportedByUsername", "ImportedByUsername");
                    bulk.ColumnMappings.Add("ImportedDateTime", "ImportedDateTime");
                    bulk.ColumnMappings.Add("ImportStatus", "ImportStatus");
                    bulk.ColumnMappings.Add("ProcessingMessage", "ProcessingMessage");
                    bulk.ColumnMappings.Add("ImportFileName", "ImportFileName");
                    bulk.ColumnMappings.Add("ImportBatchId", "ImportBatchId");

                    bulk.BulkCopyTimeout = 600;
                    bulk.WriteToServer(dt);
                }
            }
        }

        private string SafeGet(IDictionary<string, string> dict, string key)
        {
            if (dict == null) return null;

            // direct lookup
            string v;
            if (dict.TryGetValue(key, out v) && !string.IsNullOrWhiteSpace(v))
                return CleanString(v);

            // heuristics
            var alternatives = new[]
            {
        key,
        key.Replace("_", " "),
        key.Replace("_", ""),
        key.ToLowerInvariant(),
        key.ToUpperInvariant()
    };

            foreach (var alt in alternatives)
            {
                string v2;
                if (dict.TryGetValue(alt, out v2) && !string.IsNullOrWhiteSpace(v2))
                    return CleanString(v2);
            }

            // final fallback: partial match
            foreach (string k in dict.Keys)
            {
                if (!string.IsNullOrWhiteSpace(k) &&
                    k.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string v3 = dict[k];
                    if (!string.IsNullOrWhiteSpace(v3))
                        return CleanString(v3);
                }
            }

            return null;
        }


        private string CleanString(string s)
        {
            if (s == null) return null;
            s = s.Trim();
            s = s.Replace("\uFEFF", ""); // BOM
            s = s.Replace("\u00A0", " "); // NBSP
            s = s.Replace("“", "\"").Replace("”", "\"");
            s = s.Replace("‘", "'").Replace("’", "'");
            return s.Trim();
        }



        private string SafeGet_OLD(IDictionary<string, string> dict, string key)
        {
            if (dict == null) return null;

            string v;
            if (dict.TryGetValue(key, out v) && !string.IsNullOrEmpty(v))
                return v;

            // Try few heuristics
            var alternatives = new[]
            {
                key.Replace("_", " "),
                key.Replace("_", ""),
                key.ToLowerInvariant(),
                key.ToUpperInvariant()
            };

            foreach (var alt in alternatives)
            {
                string v2;
                if (dict.TryGetValue(alt, out v2) && !string.IsNullOrEmpty(v2))
                    return v2;
            }

            // final fallback: header that contains token
            foreach (var k in dict.Keys)
            {
                if (!string.IsNullOrEmpty(k) && k.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return dict[k];
            }

            return null;
        }



        // Optional: call stored proc to normalize staging -> final
        private void ProcessStagingToFinal()
        {
            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("dbo.Process_StagingToFinal_FromJson", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                cmd.CommandTimeout = 600;
                cmd.ExecuteNonQuery();
            }
        }
    }
}