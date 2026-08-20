<!-- ========================================================================
     ITSM Menu Integration for Dristi.Master
     When:    2026-08-20
     What:    HTML snippet to add to the sidebar menu in Dristi.Master
     Why:     Phase 1B requires agent-facing navigation to the ITSM ticket queue
              and create ticket pages.

     INSTRUCTIONS:
     Add the following <li> block inside the sidebar <ul class="nav side-menu">
     in Dristi.Master, after the existing "Home" menu item and before the
     "Forms" menu item.

     This provides:
     - "ITSM Dashboard" → ~/ITSM/UI/TicketList.aspx (agent queue)
     - "Create Ticket" → ~/ITSM/UI/CreateTicket.aspx (new ticket form)

     The menu follows the exact same Gentelella sidebar pattern used by
     existing menu items (Forms, Approvals, Reports, etc.).
     ======================================================================== -->

<!-- START: ITSM Menu Block — Add this to Dristi.Master sidebar -->
<li>
    <a href="#"><i class="fa fa-life-ring"></i> ITSM <span class="fa fa-chevron-down"></span></a>
    <ul class="nav child_menu">
        <li>
            <a href="~/ITSM/UI/TicketList.aspx">
                <i class="fa fa-list"></i> Agent Queue
            </a>
        </li>
        <li>
            <a href="~/ITSM/UI/CreateTicket.aspx">
                <i class="fa fa-plus-circle"></i> Create Ticket
            </a>
        </li>
    </ul>
</li>
<!-- END: ITSM Menu Block -->
