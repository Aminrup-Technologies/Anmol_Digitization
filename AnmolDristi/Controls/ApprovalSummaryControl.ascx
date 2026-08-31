<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ApprovalSummaryControl.ascx.cs" Inherits="AnmolDristi.Controls.ApprovalSummaryControl" %>
<%@ Register Src="~/Controls/VisitWindowControl.ascx" TagPrefix="vms" TagName="VisitWindow" %>
<%@ Register Src="~/Controls/StatusBadgeControl.ascx" TagPrefix="vms" TagName="StatusBadge" %>
<div class="vms-approval-summary" role="region" aria-label="Approval summary">
    <asp:Panel ID="pnlStatus" runat="server" Visible="false" CssClass="vms-approval-summary-status">
        <vms:StatusBadge ID="ucStatus" runat="server" />
    </asp:Panel>
    <vms:VisitWindow ID="ucVisitWindow" runat="server" />
</div>
