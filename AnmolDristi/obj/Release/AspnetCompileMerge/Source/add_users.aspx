<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="add_users.aspx.cs" Inherits="AnmolDristi.add_users" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <%--<div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label></h3>
                </div>
            </div>--%>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Add New User"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:ValidationSummary ID="ValidationSummary1" runat="server" CssClass="text-danger" Visible="false" />
                            <asp:Panel ID="Panel1" runat="server" CssClass="form-vertical">
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="txtEmployeeCode">Employee Code:</label>
                                    <div class="col-md-4">
                                        <asp:TextBox ID="txtEmployeeCode" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="50" />
                                        <asp:RequiredFieldValidator ID="RFV_txtEmployeeCode" runat="server" ControlToValidate="txtEmployeeCode" ErrorMessage="Employee Code is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="txtEmployeeName">Employee Name:</label>
                                    <div class="col-md-4">
                                        <asp:TextBox ID="txtEmployeeName" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="50" />
                                        <asp:RequiredFieldValidator ID="RFV_txtEmployeeName" runat="server" ControlToValidate="txtEmployeeName" ErrorMessage="Employee Name is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlRegionId">Region:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlRegionId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator8" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Region is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlBranchId">Branch:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlBranchId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RFV_ddlBranchId" runat="server" ControlToValidate="ddlBranchId" InitialValue="" ErrorMessage="Branch is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlPlantId">Plant:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlPlantId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RFV_ddlPlantId" runat="server" ControlToValidate="ddlPlantId" InitialValue="" ErrorMessage="Plant is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlEmployeeStatus">Employee Status:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlEmployeeStatus" runat="server" CssClass="form-control form-control-sm rounded">
                                            <asp:ListItem Value="Active">Active</asp:ListItem>
                                            <asp:ListItem Value="Blocked">Blocked</asp:ListItem>
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator ID="RFV_ddlEmployeeStatus" runat="server" ControlToValidate="ddlEmployeeStatus" InitialValue="" ErrorMessage="Status is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlCompanyId">Company:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlCompanyId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RFV_ddlCompanyId" runat="server" ControlToValidate="ddlCompanyId" InitialValue="" ErrorMessage="Company is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlDepartmentId">Department:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlDepartmentId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RFV_ddlDepartmentId" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Department is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlGradeId">Grade:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlGradeId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Grade is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlSubDepartmentId">Sub-Department:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlSubDepartmentId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Sub-Department is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="txtDOJ">Date of Joining:</label>
                                    <div class="col-md-4">
                                        <asp:TextBox ID="txtDOJ" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Date of Joining is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlCategoryId">Category:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlCategoryId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RFV_ddlCategoryId" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Category is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="txtEmail">Email:</label>
                                    <div class="col-md-4">
                                        <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="50" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator5" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Email is required" CssClass="text-danger" />
                                        <asp:RegularExpressionValidator ID="REV_txtEmail" runat="server" ControlToValidate="txtEmail" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Invalid Email Format" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="txtMobile">Mobile:</label>
                                    <div class="col-md-4">
                                        <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="10" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator6" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Mobile is required" CssClass="text-danger" />
                                        <asp:RegularExpressionValidator ID="REV_txtMobile" runat="server" ControlToValidate="txtMobile" ValidationExpression="^\d{10}$" ErrorMessage="Invalid Mobile Number" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlGender">Gender:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlGender" runat="server" CssClass="form-control form-control-sm rounded">
                                            <asp:ListItem Value="Male">Male</asp:ListItem>
                                            <asp:ListItem Value="Female">Female</asp:ListItem>
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Gender is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlDivisionId">Division:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlDivisionId" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator9" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Division is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="txtPassword">Password:</label>
                                    <div class="col-md-4">
                                        <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Password" MaxLength="50" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator10" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Password is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label col-md-2" for="ddlCreationMode">Creation Mode:</label>
                                    <div class="col-md-4">
                                        <asp:DropDownList ID="ddlCreationMode" runat="server" CssClass="form-control form-control-sm rounded">
                                            <asp:ListItem Value="Single">Single</asp:ListItem>
                                            <asp:ListItem Value="Bulk">Bulk</asp:ListItem>
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator11" runat="server" ControlToValidate="ddlDepartmentId" InitialValue="" ErrorMessage="Creation Mode is required" CssClass="text-danger" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <div class="col-md-offset-2 col-md-4">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary" />
                                    </div>
                                </div>
                            </asp:Panel>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
