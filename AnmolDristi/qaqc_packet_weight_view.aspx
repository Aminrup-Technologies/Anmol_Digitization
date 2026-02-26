<%@ Page Title="AIL | View Packet Weights" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_packet_weight_view.aspx.cs" Inherits="AnmolDristi.qaqc_packet_weight_view" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Center alignment for grid cells */
        .valign-middle th, .valign-middle td {
            vertical-align: middle !important;
            text-align: center;
        }

        /* Modal header styling to match Gentelella's dark theme */
        .modal-header.dark-header {
            background-color: #2A3F54;
            color: #ECF0F1;
            border-bottom: 1px solid #E6E9ED;
        }

            .modal-header.dark-header .close {
                color: #ECF0F1;
                opacity: 0.8;
            }

                .modal-header.dark-header .close:hover {
                    opacity: 1;
                }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script type="text/javascript">
        function openDetailsModal() {
            $('#detailsModal').modal('show');
        }
    </script>

    <div class="right_col" role="main">
        <div class="">
            <div class="page-title">
                <div class="title_left">
                    <h3>Packet Weight <small>Inspection Report</small></h3>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12 col-xs-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2><i class="fa fa-filter"></i>Search Filters </h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-2 col-sm-4 col-xs-6 form-group has-feedback">
                                    <asp:Label runat="server" Text="From Date" CssClass="control-label"></asp:Label>
                                    <asp:TextBox ID="txtFromDate" runat="server" CssClass="form-control has-feedback-left" TextMode="Date"></asp:TextBox>
                                    <span class="fa fa-calendar form-control-feedback left" aria-hidden="true"></span>
                                </div>

                                <div class="col-md-2 col-sm-4 col-xs-6 form-group has-feedback">
                                    <asp:Label runat="server" Text="To Date" CssClass="control-label"></asp:Label>
                                    <asp:TextBox ID="txtToDate" runat="server" CssClass="form-control has-feedback-left" TextMode="Date"></asp:TextBox>
                                    <span class="fa fa-calendar form-control-feedback left" aria-hidden="true"></span>
                                </div>

                                <div class="col-md-2 col-sm-4 col-xs-12 form-group">
                                    <asp:Label runat="server" Text="Plant" CssClass="control-label"></asp:Label>
                                    <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                </div>

                                <div class="col-md-2 col-sm-4 col-xs-12 form-group">
                                    <asp:Label runat="server" Text="Line" CssClass="control-label"></asp:Label>
                                    <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                </div>

                                <div class="col-md-2 col-sm-4 col-xs-12 form-group">
                                    <asp:Label runat="server" Text="Category" CssClass="control-label"></asp:Label>
                                    <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                </div>

                                <div class="col-md-2 col-sm-4 col-xs-12 form-group">
                                    <asp:Label runat="server" Text="Brand" CssClass="control-label"></asp:Label>
                                    <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12 form-group">
                                    <asp:Label runat="server" Text="SKU" CssClass="control-label"></asp:Label>
                                    <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control"></asp:DropDownList>
                                </div>

                                <div class="col-md-2 col-sm-2 col-xs-12 form-group">
                                    <br />
                                    <asp:LinkButton ID="lnkBtnSearch" runat="server" CssClass="btn btn-success" OnClick="lnkBtnSearch_Click">
    <i class="fa fa-search"></i> Fetch Records
</asp:LinkButton>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 col-xs-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2><i class="fa fa-table"></i>Inspection Records</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                <li><a class="close-link"><i class="fa fa-close"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content text-center">
                            <asp:Label ID="lblMessage" runat="server" ForeColor="#E74C3C" Font-Bold="true"></asp:Label>

                            <div class="table-responsive">
                                <asp:GridView ID="gvHeaders" runat="server" AutoGenerateColumns="False"
                                    CssClass="table table-striped table-bordered jambo_table bulk_action valign-middle"
                                    GridLines="None"
                                    EmptyDataText="<div class='alert alert-warning text-center' style='margin: 10px;'><i class='fa fa-info-circle'></i> No records found for the selected filters.</div>"
                                    OnRowCommand="gvHeaders_RowCommand">
                                    <Columns>
                                        <asp:BoundField DataField="EntryDate" HeaderText="Date" DataFormatString="{0:dd-MMM-yyyy}" />

                                        <asp:TemplateField HeaderText="Shift">
                                            <ItemTemplate>
                                                <span class="badge bg-green"><%# Eval("Shift") %></span>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:BoundField DataField="PlantName" HeaderText="Plant" />
                                        <asp:BoundField DataField="Line" HeaderText="Line" />
                                        <asp:BoundField DataField="SKUName" HeaderText="SKU" ItemStyle-Font-Bold="true" />

                                        <asp:BoundField DataField="StandardWeight" HeaderText="Std Wgt (g)" DataFormatString="{0:0.00}" />
                                        <asp:BoundField DataField="MinWeight" HeaderText="Min (g)" DataFormatString="{0:0.00}" />
                                        <asp:BoundField DataField="MaxWeight" HeaderText="Max (g)" DataFormatString="{0:0.00}" />

                                        <asp:TemplateField HeaderText="Avg (g)">
                                            <ItemTemplate>
                                                <strong class="text-primary"><%# Eval("AvgWeight", "{0:0.00}") %></strong>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Give-Away %">
                                            <ItemTemplate>
                                                <span class='<%# Convert.ToDecimal(Eval("GiveAwayPercent")) > 0 ? "text-danger font-weight-bold" : "text-success font-weight-bold" %>'>
                                                    <%# Eval("GiveAwayPercent", "{0:0.00}%") %>
                                                </span>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Action">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lnkView" runat="server"
                                                    CommandName="ViewDetails"
                                                    CommandArgument='<%# Eval("PacketHdrID") %>'
                                                    CssClass="btn btn-info btn-xs" ToolTip="View Interval Details">
                                                    <i class="fa fa-list-alt"></i> Details
                                                </asp:LinkButton>
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

    <div class="modal fade" id="detailsModal" tabindex="-1" role="dialog" aria-labelledby="detailsModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-md" role="document">
            <div class="modal-content">
                <div class="modal-header dark-header">
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                    <h4 class="modal-title" id="detailsModalLabel"><i class="fa fa-balance-scale"></i>Packet Weight Details</h4>
                </div>
                <div class="modal-body">
                    <div class="table-responsive">
                        <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="False"
                            CssClass="table table-striped table-bordered valign-middle">
                            <HeaderStyle BackColor="#f5f7fa" ForeColor="#333" />
                            <Columns>
                                <asp:BoundField DataField="PacketNo" HeaderText="Packet No." />
                                <asp:TemplateField HeaderText="Interval Window">
                                    <ItemTemplate>
                                        <i class="fa fa-clock-o text-muted"></i><%# Eval("IntervalStart") %> - <%# Eval("IntervalEnd") %>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Weight (g)">
                                    <ItemTemplate>
                                        <strong><%# Eval("PacketWeight", "{0:0.00}") %></strong>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-default" data-dismiss="modal"><i class="fa fa-times"></i>Close</button>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
