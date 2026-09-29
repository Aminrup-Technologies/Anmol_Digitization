using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_TicketStatus — New, Pending Approval, In Progress, Resolved, Closed, etc.
    /// </summary>
    [Table("MST_TicketStatus", Schema = "itsm")]
    public class TicketStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// When true, the ticket is complete for SLA polling (e.g. Resolved / Closed / Rejected).
        /// </summary>
        public bool IsTerminal { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public TicketStatus()
        {
            Tickets = new HashSet<Ticket>();
            IsActive = true;
        }
    }
}
