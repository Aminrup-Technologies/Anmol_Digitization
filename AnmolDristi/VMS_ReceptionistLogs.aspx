<%@ Page Title="Visitor Logs & Reports" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_ReceptionistLogs.aspx.cs" Inherits="AnmolDristi.VMS_ReceptionistLogs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style type="text/css">
        .filter-panel {
            background: #fff;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }

        .chart-container {
            background: #fff;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }

        .grid-container {
            background: #fff;
            padding: 20px;
            border-radius: 5px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="page-title">
                <div class="title_left">
                    <h3>Visitor Logs & Reports</h3>
                </div>
            </div>
            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12">
                    <div class="filter-panel">
                        <div class="row">
                            <div class="col-md-2">
                                <label>Select Branch</label>
                                <asp:DropDownList ID="ddlBranch" runat="server" CssClass="form-control"></asp:DropDownList>
                            </div>
                            <div class="col-md-2">
                                <label>From Date</label>
                                <asp:TextBox ID="txtFromDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                            </div>
                            <div class="col-md-2">
                                <label>To Date</label>
                                <asp:TextBox ID="txtToDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                            </div>
                            <div class="col-md-3">
                                <label>Status</label>
                                <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control">
                                    <asp:ListItem Text="Default (Excl. Cancelled/Rejected)" Value="ActiveDefault" Selected="True"></asp:ListItem>
                                    <asp:ListItem Text="Scheduled (Awaiting Check-In)" Value="Scheduled"></asp:ListItem>
                                    <asp:ListItem Text="Checked-In (Inside Premises)" Value="Checked-In"></asp:ListItem>
                                    <asp:ListItem Text="Checked-Out" Value="Checked-Out"></asp:ListItem>
                                    <asp:ListItem Text="Cancelled / Rejected" Value="Cancelled"></asp:ListItem>
                                    <asp:ListItem Text="All Records (Include Cancelled)" Value="All"></asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="col-md-3" style="margin-top: 25px;">
                                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnSearch_Click" />
                                <asp:Button ID="btnExport" runat="server" Text="Export Excel" CssClass="btn btn-success" OnClick="btnExport_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="chart-container">
                        <h4>Visitor Trends (By Category)</h4>
                        <canvas id="visitorTrendChart" height="250"></canvas>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="grid-container">
                        <asp:UpdatePanel ID="UpdatePanelGrid" runat="server">
                            <ContentTemplate>
                                <asp:GridView ID="gvVisitorLogs" runat="server" AutoGenerateColumns="False"
                                    CssClass="table table-striped table-bordered" OnRowCommand="gvVisitorLogs_RowCommand" DataKeyNames="LogID">
                                    <Columns>
                                        <asp:BoundField DataField="LogID" HeaderText="ID" ItemStyle-Width="50px" />

                                        <asp:TemplateField HeaderText="Branch">
                                            <ItemTemplate>
                                                <strong><%# Eval("Branch_Description") %></strong><br />
                                                <span class="text-muted" style="font-size: 12px;"><i class="fa fa-building"></i>ID: <%# Eval("Branch_ID") %></span>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Visitor Profile">
                                            <ItemTemplate>
                                                <div style="display: flex; align-items: center;">
                                                    <img src='<%# string.IsNullOrEmpty(Eval("PhotoPath").ToString()) ? "WebData/No_Image.jpg" : Eval("PhotoPath") %>'
                                                        alt="Photo" style="width: 80px; height: 80px; border-radius: 50%; object-fit: cover; margin-right: 12px; border: 1px solid #ddd;" />
                                                    <div>
                                                        <strong><%# Eval("VisitorName") %></strong>
                                                        <span class="label label-info" style="margin-left: 5px;"><%# Eval("PassCategory") %></span><br />
                                                        <span class="text-muted" style="font-size: 12px;"><i class="fa fa-phone"></i>&nbsp;<%# Eval("VisitorMobile") %><br />
                                                            <i class="fa fa-envelope"></i>&nbsp;<%# Eval("VisitorEmail") %></span></br>
                                                        <span class="text-muted" style="font-size: 12px;"><i class="fa fa-briefcase"></i>&nbsp;<%# Eval("VisitorCompany") %></span>
                                                    </div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Host Details">
                                            <ItemTemplate>
                                                <strong><%# Eval("HostName") %></strong><br />
                                                <span class="text-muted" style="font-size: 12px;"><i class="fa fa-phone"></i>&nbsp;<%# Eval("HostMobile") %><br />
                                                    <i class="fa fa-envelope"></i>&nbsp;<%# Eval("HostEmail") %></span><br />
                                                <span class="text-muted" style="font-size: 12px;"><i class="fa fa-user"></i>&nbsp;Code: <%# Eval("HostEmployeeCode") %></span>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Timeline">
                                            <ItemTemplate>
                                                <div style="font-size: 11px; line-height: 1.4;">
                                                    <div class="text-muted"><strong>Req:</strong> <%# Eval("CreatedDate", "{0:dd-MMM HH:mm:tt}") %></div>
                                                    <div class="text-primary"><strong>App:</strong> <%# Eval("ApprovedOn") != DBNull.Value ? Eval("ApprovedOn", "{0:dd-MMM HH:mm:tt}") : "Pending" %></div>
                                                    <div class="text-success"><strong>In:</strong> <%# Eval("ActualInTime") != DBNull.Value ? Eval("ActualInTime", "{0:dd-MMM HH:mm:tt}") : "--:--" %></div>
                                                    <div class="text-warning"><strong>Out:</strong> <%# Eval("ActualOutTime") != DBNull.Value ? Eval("ActualOutTime", "{0:dd-MMM HH:mm:tt}") : "--:--" %></div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Status" ItemStyle-Width="100px">
                                            <ItemTemplate>
                                                <strong><%# Eval("VisitStatus") %></strong>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Actions" ItemStyle-Width="130px" ItemStyle-CssClass="text-center">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lnkView" runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("LogID") %>' CssClass="btn btn-info btn-xs" ToolTip="View Details"><i class="fa fa-eye"></i></asp:LinkButton>

                                                <asp:LinkButton ID="lnkApprove" runat="server" CommandName="ApproveVisit" CommandArgument='<%# Eval("LogID") %>'
                                                    CssClass="btn btn-success btn-xs" ToolTip="Force Approve"
                                                    Visible='<%# Eval("ApprovalStatus").ToString() == "Pending" %>'
                                                    OnClientClick="return confirm('Are you sure you want to approve this visitor?');">
                                                    <i class="fa fa-check"></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="lnkCheckIn" runat="server" CommandName="CheckInVisit" CommandArgument='<%# Eval("LogID") %>'
                                                    CssClass="btn btn-primary btn-xs" ToolTip="Manual Check-In"
                                                    Visible='<%# Eval("VisitStatus").ToString() == "Scheduled" && Eval("ApprovalStatus").ToString() == "Approved" %>'
                                                    OnClientClick="return confirm('Are you sure you want to manually Check-In this visitor?');">
                                                    <i class="fa fa-sign-in"></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="lnkCheckOut" runat="server" CommandName="CheckOutVisit" CommandArgument='<%# Eval("LogID") %>'
                                                    CssClass="btn btn-warning btn-xs" ToolTip="Manual Check-Out"
                                                    Visible='<%# Eval("VisitStatus").ToString() == "Checked-In" %>'
                                                    OnClientClick="return confirm('Are you sure you want to manually Check-Out this visitor?');">
                                                    <i class="fa fa-sign-out"></i>
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <div class="alert alert-warning">No records found for the selected criteria.</div>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                            </ContentTemplate>
                            <Triggers>
                                <asp:AsyncPostBackTrigger ControlID="btnSearch" EventName="Click" />
                            </Triggers>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="detailsModal" tabindex="-1" role="dialog" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <asp:UpdatePanel ID="UpdatePanelModal" runat="server">
                    <ContentTemplate>
                        <div class="modal-header">
                            <button type="button" class="close" data-dismiss="modal"><span aria-hidden="true">×</span></button>
                            <h4 class="modal-title">Visitor Pass Details</h4>
                        </div>
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-md-4 text-center">
                                    <asp:Image ID="imgMdlVisitor" runat="server" CssClass="img-responsive img-thumbnail"
                                        Style="width: 100%; max-width: 250px; border-radius: 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.1);" />
                                    <br />
                                    <br />
                                    <span class="label label-primary" style="font-size: 14px; padding: 5px 15px;">Status:
                                        <asp:Label ID="lblMdlStatusBadge" runat="server"></asp:Label>
                                    </span>
                                </div>

                                <div class="col-md-4">
                                    <h5 style="border-bottom: 2px solid #eee; padding-bottom: 5px; color: #1ABB9C;">Visitor Info</h5>
                                    <p>
                                        <strong>Name:</strong>
                                        <asp:Label ID="lblMdlName" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Mobile:</strong>
                                        <asp:Label ID="lblMdlMobile" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Email:</strong>
                                        <asp:Label ID="lblMdlEmail" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Company:</strong>
                                        <asp:Label ID="lblMdlCompany" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Category:</strong>
                                        <asp:Label ID="lblMdlCategory" runat="server"></asp:Label>
                                    </p>
                                </div>

                                <div class="col-md-4">
                                    <h5 style="border-bottom: 2px solid #eee; padding-bottom: 5px; color: #1ABB9C;">Visit Info</h5>
                                    <p>
                                        <strong>Host Code:</strong>
                                        <asp:Label ID="lblMdlHost" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Purpose:</strong>
                                        <asp:Label ID="lblMdlPurpose" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Valid Upto:</strong>
                                        <asp:Label ID="lblMdlValid" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Status:</strong>
                                        <asp:Label ID="lblMdlStatus" runat="server"></asp:Label>
                                    </p>
                                    <p>
                                        <strong>Meal Required:</strong>
                                        <asp:Label ID="lblMdlMeal" runat="server"></asp:Label>
                                    </p>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        var myChart = null;

        // Notice the updated parameters matching our C# output
        function renderChart(labels, internalData, meetingData, deliveryData, serviceData, interviewData) {
            try {
                var ctx = document.getElementById('visitorTrendChart').getContext('2d');

                if (myChart != null) {
                    myChart.destroy(); // Clear old chart on postback
                }

                myChart = new Chart(ctx, {
                    type: 'bar',
                    data: {
                        labels: JSON.parse(labels),
                        datasets: [
                            {
                                label: 'Internal (Work Here)',
                                data: JSON.parse(internalData),
                                backgroundColor: 'rgba(26, 187, 156, 0.8)', // Green
                                borderWidth: 1
                            },
                            {
                                label: 'Meetings',
                                data: JSON.parse(meetingData),
                                backgroundColor: 'rgba(52, 152, 219, 0.8)', // Blue
                                borderWidth: 1
                            },
                            {
                                label: 'Deliveries',
                                data: JSON.parse(deliveryData),
                                backgroundColor: 'rgba(243, 156, 18, 0.8)', // Orange
                                borderWidth: 1
                            },
                            {
                                label: 'Services',
                                data: JSON.parse(serviceData),
                                backgroundColor: 'rgba(155, 89, 182, 0.8)', // Purple
                                borderWidth: 1
                            },
                            {
                                label: 'Interviews',
                                data: JSON.parse(interviewData),
                                backgroundColor: 'rgba(231, 76, 60, 0.8)', // Red
                                borderWidth: 1
                            }
                        ]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            x: {
                                stacked: true,
                                ticks: {
                                    autoSkip: false,
                                    maxRotation: 90,
                                    minRotation: 45
                                }
                            },
                            y: {
                                stacked: true,
                                beginAtZero: true,
                                ticks: { stepSize: 1 }
                            }
                        }
                    }
                });
            } catch (error) {
                console.error("Chart Rendering Error: ", error);
            }
        }

        function showModal() {
            $('#detailsModal').modal('show');
        }
    </script>
</asp:Content>
