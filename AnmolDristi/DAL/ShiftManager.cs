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

        public string GetCurrentShiftType()
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
    }
}