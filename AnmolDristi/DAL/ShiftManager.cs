using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AnmolDristi.DAL
{
    public class ShiftManager
    {
        private List<Shift> shifts;

        public ShiftManager()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            shifts = LoadShiftsFromDatabase(connectionString);
        }

        public string GetCurrentShiftType_OLD()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            foreach (var shift in shifts)
            {
                if (IsCurrentTimeInShift(currentTime, shift.StartTime, shift.EndTime))
                {
                    return shift.ShiftName;
                }
            }

            return "No Shift"; // Default if no matching shift is found
        }

        public string GetCurrentShiftType()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            DataTable shiftTable = GetShiftDataFromDB();

            foreach (DataRow row in shiftTable.Rows)
            {
                string shiftName = row["ShiftName"].ToString();
                TimeSpan startTime = TimeSpan.Parse(row["StartTime"].ToString());
                TimeSpan endTime = TimeSpan.Parse(row["EndTime"].ToString());

                if (startTime < endTime)
                {
                    if (currentTime >= startTime && currentTime < endTime)
                        return shiftName;
                }
                else
                {
                    if (currentTime >= startTime || currentTime < endTime)
                        return shiftName;
                }
            }

            return "Unknown";
        }

        private DataTable GetShiftDataFromDB()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT ShiftName, StartTime, EndTime FROM ShiftMasterTable", conn))
                {
                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }



        private bool IsCurrentTimeInShift(TimeSpan currentTime, TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime <= endTime)
            {
                // Normal shift within the same day
                return currentTime >= startTime && currentTime < endTime;
            }
            else
            {
                // Overnight shift (e.g., 10 PM to 6 AM)
                return currentTime >= startTime || currentTime < endTime;
            }
        }

        private List<Shift> LoadShiftsFromDatabase(string connectionString)
        {
            List<Shift> shifts = new List<Shift>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT ShiftId, ShiftName, StartTime, EndTime FROM ShiftMasterTable", conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Shift shift = new Shift
                        {
                            ShiftId = reader.GetInt32(0),
                            ShiftName = reader.GetString(1),
                            StartTime = reader.GetTimeSpan(2),
                            EndTime = reader.GetTimeSpan(3)
                        };
                        shifts.Add(shift);
                    }
                }
            }

            return shifts;
        }

        public class ShiftInfo
        {
            public string ShiftName { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }


        public ShiftInfo GetCurrentShiftInfo()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            using (SqlConnection conn =
                new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT ShiftName, StartTime, EndTime
        FROM ShiftMasterTable
        ORDER BY ShiftId", conn))
            {
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shiftName = reader.GetString(0);
                        TimeSpan startTime = reader.GetTimeSpan(1);
                        TimeSpan endTime = reader.GetTimeSpan(2);

                        // Normal shift
                        if (startTime < endTime)
                        {
                            if (currentTime >= startTime && currentTime < endTime)
                            {
                                return new ShiftInfo
                                {
                                    ShiftName = shiftName,
                                    StartTime = startTime,
                                    EndTime = endTime
                                };
                            }
                        }
                        // Overnight shift (C)
                        else
                        {
                            if (currentTime >= startTime || currentTime < endTime)
                            {
                                return new ShiftInfo
                                {
                                    ShiftName = shiftName,
                                    StartTime = startTime,
                                    EndTime = endTime
                                };
                            }
                        }
                    }
                }
            }

            throw new Exception("Unable to determine current shift.");
        }

    }
}