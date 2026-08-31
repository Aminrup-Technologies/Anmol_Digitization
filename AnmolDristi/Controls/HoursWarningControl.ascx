<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="HoursWarningControl.ascx.cs" Inherits="AnmolDristi.Controls.HoursWarningControl" %>
<div id="pnlHoursWarning" runat="server"
     class="vms-hours-warning"
     data-vms-hours-warning="1"
     role="status"
     aria-live="polite"
     aria-atomic="true">
    <span class="vms-hours-warning-title">
        <i class="fa fa-exclamation-triangle" aria-hidden="true"></i>
        Working hours notice
    </span>
    <p class="vms-hours-warning-body" data-vms-hours-warning-body="1"><asp:Literal ID="litText" runat="server"></asp:Literal></p>
</div>
