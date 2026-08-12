using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.VMS_PlantTimings — facility open/close hours by branch and day of week.
    /// Used for business-hours context alongside MST_Calendar SLA calculations.
    /// </summary>
    [Table("VMS_PlantTimings", Schema = "dbo")]
    public class PlantTimings
    {
        [Key]
        [Column(Order = 0)]
        public int Branch_ID { get; set; }

        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(20)]
        public string DayOfWeek { get; set; }

        public TimeSpan? OpenTime { get; set; }

        public TimeSpan? CloseTime { get; set; }
    }
}
