using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.DAL
{
    /// <summary>
    /// EF6 DbContext for the ITSM modular monolith.
    /// Uses the shared <c>DbConn</c> connection string; new tables live in the <c>itsm</c> schema.
    /// Legacy <c>dbo</c> masters are read-only — <see cref="SaveChanges"/> rejects any tracked writes.
    /// When: 2026-08-21
    /// What: Added read-only Company (dbo.MST_Company) for ticket tenant display name.
    /// Why: Ticket.CompanyId snapshots MST_UserMaster.CompanyId (= MST_Company.Company_ID).
    /// When: 2026-08-22
    /// What: Added ApprovalConfiguration DbSet for configurable IT Head approval policy.
    /// Why: Prompt 1.6 — IT Head approver is administratively configurable, not hard-coded.
    /// </summary>
    public class ItsmDbContext : DbContext
    {
        public ItsmDbContext()
            : base("name=DbConn")
        {
            Configuration.LazyLoadingEnabled = false;
        }

        // --- Legacy dbo masters (read-only) ---

        public DbSet<UserMaster> UserMasters { get; set; }

        public DbSet<Plant> Plants { get; set; }

        public DbSet<Branch> Branches { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Calendar> Calendars { get; set; }

        public DbSet<PlantTimings> PlantTimings { get; set; }

        /// <summary>
        /// Read-only dbo.MST_Company. Join on Company_ID (not Id) — that is the value stored on UserMaster/Ticket.CompanyId.
        /// </summary>
        public DbSet<Company> Companies { get; set; }

        // --- ITSM masters (itsm schema) ---

        public DbSet<RequestType> RequestTypes { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<SubCategory> SubCategories { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<Impact> Impacts { get; set; }

        public DbSet<Urgency> Urgencies { get; set; }

        public DbSet<Priority> Priorities { get; set; }

        public DbSet<PriorityMatrix> PriorityMatrices { get; set; }

        public DbSet<TicketStatus> TicketStatuses { get; set; }

        public DbSet<SupportMapping> SupportMappings { get; set; }

        /// <summary>
        /// Global IT Head approver configuration. Only one active row permitted.
        /// </summary>
        public DbSet<ApprovalConfiguration> ApprovalConfigurations { get; set; }

        // --- ITSM transactional ---

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<TicketComment> TicketComments { get; set; }

        public DbSet<TicketAttachment> TicketAttachments { get; set; }

        public DbSet<TicketApproval> TicketApprovals { get; set; }

        /// <summary>
        /// Immutable ITGC audit trail for ticket mutations (what / who / when).
        /// </summary>
        public DbSet<TicketAuditLog> TicketAuditLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("itsm");
            // Disable global cascade deletes for required Foreign Keys
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.OneToManyCascadeDeleteConvention>();
            // Ticket.CompanyId is an integer snapshot of MST_Company.Company_ID, not an FK to Company.Id.
            // No navigation is mapped; do not invent a relationship.
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            AssertNoLegacyMasterWrites();
            return base.SaveChanges();
        }

        /// <summary>
        /// Blocks Added / Modified / Deleted states for legacy dbo.* entities.
        /// </summary>
        private void AssertNoLegacyMasterWrites()
        {
            var hasLegacyWrite = ChangeTracker.Entries()
                .Any(e =>
                    (e.State == EntityState.Added
                     || e.State == EntityState.Modified
                     || e.State == EntityState.Deleted)
                    && IsLegacyMaster(e.Entity));

            if (hasLegacyWrite)
            {
                throw new InvalidOperationException(
                    "CRITICAL: The ITSM module is strictly forbidden from modifying legacy dbo.* master tables.");
            }
        }

        private static bool IsLegacyMaster(object entity)
        {
            // Use 'is' so EF dynamic proxies still match the legacy CLR types.
            return entity is UserMaster
                || entity is Plant
                || entity is Branch
                || entity is Department
                || entity is Calendar
                || entity is PlantTimings
                || entity is Company;
        }
    }
}
