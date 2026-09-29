<%@ Page Title="Monthly Claims Approval" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="ClaimsSubmitted.aspx.cs" Inherits="AnmolDristi.ClaimsSubmitted" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .meta {
            font-size: 0.85em;
            color: #666;
        }

        .panel-details {
            border: 1px solid #e9ecef;
            padding: 10px;
            margin-top: 8px;
            background: #fff;
            box-shadow: none;
        }

            .panel-details .table {
                margin-bottom: 8px;
            }

        .highlight-current {
            background: #e8f7ff !important;
            font-weight: 600;
        }

        .rejected-row {
            color: #c82333;
            font-weight: 600;
        }

        .not-required {
            opacity: 0.6;
        }

        .action-link {
            color: #007bff;
            cursor: pointer;
            text-decoration: underline;
        }
    </style>

    <script type="text/javascript">
  function toggleRowPanel(btn) {
    var panelId = btn.getAttribute('data-panelid'); // e.g. "pnlDetails_11"
    // try exact id first (fast)
    var panel = document.getElementById(panelId);
    if (!panel) {
      // try suffix match (handles ASP.NET prefixes)
      panel = document.querySelector('[id$="' + CSSescape(panelId) + '"]');
    }
    if (!panel) {
      // fallback: any element whose id contains the panelId (most permissive)
      panel = document.querySelector('[id*="' + CSSescape(panelId) + '"]');
    }
    console.log('panel lookup for', panelId, 'found element:', panel);
    if (!panel) {
      alert('panel not found for ' + panelId + ' — check server-side Panel.ClientIDMode or inspect DOM');
      return;
    }
    if (panel.style.display === 'none' || panel.style.display === '') {
      panel.style.display = 'block';
      btn.innerText = 'Hide details';
    } else {
      panel.style.display = 'none';
      btn.innerText = 'Show details';
    }
  }

  // small helper to escape CSS selector special chars
  function CSSescape(id) {
    return id.replace(/([ #;&,.+*~\':"!^$[\]()=>|\/@])/g,'\\$1');
  }
</script>


</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container-fluid mt-3">
            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel shadow-sm rounded-2">
                        <div class="x_title d-flex justify-content-between align-items-center bg-primary text-white p-2 rounded-top">
                            <h4 class="mb-0">
                                <i class="fa fa-check-circle">&nbsp;</i>Monthly Claims Approval
                            </h4>
                            <ul class="nav navbar-right panel_toolbox mb-0">
                                <li>
                                    <a class="text-white collapse-link">
                                        <i class="fa fa-chevron-up"></i>
                                    </a>
                                </li>
                            </ul>
                        </div>

                        <div class="x_content bg-light p-3 rounded-bottom">
                            <div class="card-box table-responsive">

                                <asp:Label ID="lblMessage" runat="server" CssClass="small mb-2 d-block" />

                                <!-- Requests grid: styled to match theme tables -->
                                <asp:GridView ID="gvRequests" runat="server" AutoGenerateColumns="False"
                                    OnRowDataBound="gvRequests_RowDataBound"
                                    AllowPaging="True" PageSize="12" OnPageIndexChanging="gvRequests_PageIndexChanging"
                                    DataKeyNames="ApprovalRequestId"
                                    CssClass="table table-striped table-bordered table-hover">
                                    <Columns>
                                        <asp:BoundField DataField="ApprovalRequestId" HeaderText="Req #" ItemStyle-Width="80px" />
                                        <asp:TemplateField HeaderText="Request / Status" ItemStyle-Width="260px">
                                            <ItemTemplate>
                                                <div><strong><%# Eval("RequestDate") %></strong></div>
                                                <div class="meta">Status: <%# Eval("Status", "{0}") %></div>
                                                <div class="meta">Total Claim: ₹ <%# Eval("TotalClaimAmount","{0:N2}") %></div>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Remarks">
                                            <ItemTemplate>
                                                <asp:Label ID="lblRemarks" runat="server" Text='<%# Eval("RequestRemarks") %>' />
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Actions" ItemStyle-Width="160px">
                                            <ItemTemplate>
                                                <a class="action-link" href="javascript:;"
                                                    data-panelid='<%# "pnlDetails_" + Eval("ApprovalRequestId") %>'
                                                    onclick="toggleRowPanel(this);">Show details</a>
                                                &nbsp;|&nbsp;
                                                <asp:HyperLink ID="hlView" runat="server" Text="Open" Target="_blank" CssClass="text-decoration-none"></asp:HyperLink>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>

                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
