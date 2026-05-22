<%@ Page Title="View UI Mappings" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ViewUiControlMappings.aspx.cs" Inherits="AnmolDristi.ViewUiControlMappings" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .mapper-box {
            height: 500px;
            overflow-y: auto;
            background: #fff;
            border: 1px solid #ddd;
        }

        .node-permission {
            color: #26B99A;
            font-weight: bold;
            margin-left: 10px;
            font-size: 12px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2><i class="fa fa-list-alt"></i>Audit Existing UI Control Mappings</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_panel">
                            <div class="x_title">
                                <h2><i class="fa fa-list-alt"></i>Existing UI Control Mappings</h2>
                                <ul class="nav navbar-right panel_toolbox">
                                    <li>
                                        <asp:HyperLink ID="lnkMapNew" runat="server" NavigateUrl="UiControlMapper.aspx" CssClass="btn btn-primary btn-sm text-white">
                    <i class="fa fa-plus"></i> Map New
                                        </asp:HyperLink>
                                    </li>
                                    <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                </ul>
                                <div class="clearfix"></div>
                            </div>

                            <div class="x_content">
                                <div class="row mb-3 align-items-end">
                                    <div class="col-md-4">
                                        <label class="font-weight-bold">Select Page to Audit:</label>
                                        <asp:DropDownList ID="ddlFilterPage" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                                        </asp:DropDownList>
                                    </div>
                                    <div class="col-md-6">
                                        <asp:Button ID="btnExpand" runat="server" Text="Expand All" CssClass="btn btn-default" OnClick="btnExpand_Click" CausesValidation="false" />
                                        <asp:Button ID="btnCollapse" runat="server" Text="Collapse All" CssClass="btn btn-default" OnClick="btnCollapse_Click" CausesValidation="false" />
                                    </div>
                                </div>

                                <div class="form-group">
                                    <asp:Label ID="lblMessage" runat="server" CssClass="font-weight-bold"></asp:Label>
                                </div>

                                <div class="mapper-box border p-3">
                                    <asp:TreeView ID="tvMappingOverview" runat="server" ShowLines="true">
                                        <NodeStyle ForeColor="#73879C" Font-Size="14px" NodeSpacing="5px" />
                                        <LeafNodeStyle ForeColor="#5A738E" />
                                        <ParentNodeStyle ForeColor="#2A3F54" Font-Bold="true" />
                                    </asp:TreeView>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        // Client-side expand/collapse for TreeView
        function expandTree() {
            var tree = document.getElementById('<%= tvMappingOverview.ClientID %>');
            var nodes = tree.getElementsByTagName('a');
            for (var i = 0; i < nodes.length; i++) {
                if (nodes[i].href.indexOf('Expand') > -1) {
                    eval(nodes[i].href);
                }
            }
        }
        function collapseTree() {
            var tree = document.getElementById('<%= tvMappingOverview.ClientID %>');
            var nodes = tree.getElementsByTagName('a');
            for (var i = 0; i < nodes.length; i++) {
                if (nodes[i].href.indexOf('Collapse') > -1) {
                    eval(nodes[i].href);
                }
            }
        }
    </script>
</asp:Content>
