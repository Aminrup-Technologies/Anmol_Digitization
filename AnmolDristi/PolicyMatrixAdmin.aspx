<%@ Page Title="Policy Matrix" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="PolicyMatrixAdmin.aspx.cs" Inherits="AnmolDristi.PolicyMatrixAdmin" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .json-textarea {
            font-family: monospace;
            min-height: 160px;
        }

        .filter-row {
            margin-bottom: 10px;
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
                            <h2>SO-TA/DA Policy Management</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <%--<div class="row filter-row">
                                <div class="col-md-2">
                                    <asp:DropDownList ID="ddlRegion" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                </div>
                                <div class="col-md-2">
                                    <asp:DropDownList ID="ddlBranch" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                </div>
                                <div class="col-md-2">
                                    <asp:DropDownList ID="ddlGrade" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                </div>
                                <div class="col-md-2">
                                    <asp:DropDownList ID="ddlDesignation" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                </div>
                                <div class="col-md-2">
                                    <asp:Button ID="btnNew" runat="server" Text="New Policy" CssClass="btn btn-primary" OnClick="btnNew_Click" />
                                </div>
                            </div>--%>

                            <div class="row filter-row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Region</label>
                                        <asp:DropDownList ID="ddlRegion" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Branch</label>
                                        <asp:DropDownList ID="ddlBranch" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Branch</label>
                                        <asp:DropDownList ID="ddlGrade" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Grade</label>
                                        <asp:DropDownList ID="ddlDesignation" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
                                    </div>
                                </div>
                                <div class="col-md-3 text-right">
                                    <asp:Button ID="btnNew" runat="server" CssClass="btn btn-primary btn-sm" Text="New Policy" OnClick="btnNew_Click" />
                                </div>
                            </div>

                            <div class="row" style="margin-top: 10px;">
                                <div class="col-md-12">
                                    <asp:GridView ID="gvPolicies" runat="server" CssClass="table table-striped table-hover table-bordered table-responsive table-sm"
                                        AutoGenerateColumns="false" OnRowCommand="gvPolicies_RowCommand">
                                        <Columns>
                                            <asp:BoundField DataField="PolicyId" HeaderText="ID" />
                                            <asp:BoundField DataField="RegionName" HeaderText="Region" />
                                            <asp:BoundField DataField="BranchName" HeaderText="Branch" />
                                            <asp:BoundField DataField="GradeName" HeaderText="Grade" />
                                            <asp:BoundField DataField="DesignationName" HeaderText="Designation" />
                                            <asp:BoundField DataField="Priority" HeaderText="Priority" />
                                            <asp:BoundField DataField="EffectiveFrom" HeaderText="Effective From" DataFormatString="{0:yyyy-MM-dd}" />
                                            <asp:BoundField DataField="EffectiveTo" HeaderText="Effective To" DataFormatString="{0:yyyy-MM-dd}" />
                                            <asp:BoundField DataField="IsActive" HeaderText="Active" DataFormatString="{0}" />
                                            <asp:TemplateField HeaderText="Actions">
                                                <ItemTemplate>
                                                    <asp:LinkButton runat="server" CommandName="EditRow" CommandArgument='<%# Eval("PolicyId") %>' CssClass="btn btn-sm btn-info">Edit</asp:LinkButton>
                                                    <asp:LinkButton runat="server" CommandName="DeleteRow" CommandArgument='<%# Eval("PolicyId") %>' OnClientClick="return confirm('Delete this policy?');" CssClass="btn btn-sm btn-danger">Delete</asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>
                        </div>

                        <!-- Bootstrap modal for Insert/Edit -->
                        <div id="policyModal" class="modal fade" role="dialog" runat="server">
                            <div class="modal-dialog modal-lg">
                                <div class="modal-content">
                                    <div class="modal-header">
                                        
                                        <h4 class="modal-title" id="modalTitle">Policy</h4>
                                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                                    </div>
                                    <div class="modal-body">
                                        <asp:HiddenField ID="hfPolicyId" runat="server" />
                                        <div class="row">
                                            <div class="col-md-3">
                                                <label>Region</label>
                                                <asp:DropDownList ID="ddlModalRegion" runat="server" CssClass="form-control form-control-sm" />
                                            </div>
                                            <div class="col-md-3">
                                                <label>Branch</label>
                                                <asp:DropDownList ID="ddlModalBranch" runat="server" CssClass="form-control form-control-sm" />
                                            </div>
                                            <div class="col-md-3">
                                                <label>Grade</label>
                                                <asp:DropDownList ID="ddlModalGrade" runat="server" CssClass="form-control form-control-sm" />
                                            </div>
                                            <div class="col-md-3">
                                                <label>Designation</label>
                                                <asp:DropDownList ID="ddlModalDesignation" runat="server" CssClass="form-control form-control-sm" />
                                            </div>
                                        </div>

                                        <div class="row" style="margin-top: 10px;">
                                            <div class="col-md-3">
                                                <label>Priority</label>
                                                <asp:TextBox ID="txtPriority" runat="server" CssClass="form-control form-control-sm" Text="1" />
                                            </div>
                                            <div class="col-md-3">
                                                <label>Effective From</label>
                                                <asp:TextBox ID="txtEffectiveFrom" runat="server" CssClass="form-control form-control-sm" TextMode="Date" />
                                            </div>
                                            <div class="col-md-3">
                                                <label>Effective To</label>
                                                <asp:TextBox ID="txtEffectiveTo" runat="server" CssClass="form-control form-control-sm" TextMode="Date" />
                                            </div>
                                            <div class="col-md-3">
                                                <label>Active</label><br />
                                                <asp:CheckBox ID="chkIsActive" runat="server" Checked="true" />
                                            </div>
                                        </div>

                                        <div style="margin-top: 10px;">
                                            <label>Policy JSON</label>
                                            <asp:TextBox ID="txtPolicyJson" runat="server" TextMode="MultiLine" CssClass="form-control form-control-sm" Rows="6"></asp:TextBox>
                                            <small class="text-muted">Example: {"MaxKM":100,"RatePerKm":2.75,"DA":100}</small>
                                        </div>
                                    </div>
                                    <div class="modal-footer">
                                        <asp:Button ID="btnSavePolicy" runat="server" CssClass="btn btn-sm btn-success" Text="Save" OnClick="btnSavePolicy_Click" />
                                        <button type="button" class="btn btn-sm btn-danger" data-dismiss="modal">Close</button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>   

    <script type="text/javascript">
        function showPolicyModal() {
            $('#policyModal').modal('show');
        }
        function hidePolicyModal() {
            $('#policyModal').modal('hide');
        }
        function showPNotify(type, title, text) {
            if (window.PNotify) {
                new PNotify({ title: title, text: text, type: type });
            } else {
                alert(title + ": " + text);
            }
        }
    </script>
</asp:Content>
