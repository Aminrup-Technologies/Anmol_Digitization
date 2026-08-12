using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.MST_Calendar — business calendar for SLA date math and analytics.
    /// </summary>
    [Table("MST_Calendar", Schema = "dbo")]
    public class Calendar
    {
        [Key]
        [Column(TypeName = "date")]
        public DateTime CalendarDate { get; set; }

        public bool IsWeekend { get; set; }

        [StringLength(10)]
        public string FinancialYear { get; set; }

        [StringLength(10)]
        public string FinancialQuarter { get; set; }

        public bool IsMonthEnd { get; set; }

        public bool IsQuarterEnd { get; set; }
    }
}
