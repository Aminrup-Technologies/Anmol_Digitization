namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System.Data.Entity.Migrations;
    using AnmolDristi.ITSM.DAL.Entities;

    internal sealed class Configuration : DbMigrationsConfiguration<AnmolDristi.ITSM.DAL.ItsmDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"ITSM\DAL\Migrations";
        }

        protected override void Seed(AnmolDristi.ITSM.DAL.ItsmDbContext context)
        {
            context.TicketStatuses.AddOrUpdate(
                s => s.Name,
                new TicketStatus { Name = "New", IsTerminal = false, IsActive = true },
                new TicketStatus { Name = "Pending Approval", IsTerminal = false, IsActive = true },
                new TicketStatus { Name = "In Progress", IsTerminal = false, IsActive = true },
                new TicketStatus { Name = "On Hold", IsTerminal = false, IsActive = true },
                new TicketStatus { Name = "Resolved", IsTerminal = false, IsActive = true },
                new TicketStatus { Name = "Closed", IsTerminal = true, IsActive = true },
                new TicketStatus { Name = "Cancelled", IsTerminal = true, IsActive = true },
                new TicketStatus { Name = "Rejected", IsTerminal = true, IsActive = true });
        }
    }
}
