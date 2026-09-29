# ITSM Menu Integration (`Dristi.Master`)

Add this block inside the Gentelella sidebar `<ul class="nav side-menu">` after Home.

Paths are app-relative. Login session required: `WORKMAN` + `USERID`.

```html
<li>
    <a href="#"><i class="fa fa-life-ring"></i> ITSM <span class="fa fa-chevron-down"></span></a>
    <ul class="nav child_menu">
        <li><a href="ITSM/UI/TicketList.aspx"><i class="fa fa-list"></i> Agent Queue</a></li>
        <li><a href="ITSM/UI/CreateTicket.aspx"><i class="fa fa-plus-circle"></i> Create Ticket</a></li>
    </ul>
</li>
```

Tenant isolation: queues and detail load tickets where `Requester.CompanyId` matches the signed-in `MST_UserMaster.CompanyId` (ITGC).
