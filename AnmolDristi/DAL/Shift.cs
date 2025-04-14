using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AnmolDristi.DAL
{
    public class Shift
    {
        public int ShiftId { get; set; }
        public string ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}