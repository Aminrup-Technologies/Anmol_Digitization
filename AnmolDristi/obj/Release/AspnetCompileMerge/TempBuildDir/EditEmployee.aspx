<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="EditEmployee.aspx.cs" Inherits="AnmolDristi.EditEmployee" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        // Email Validation
        function validateEmail(input) {
            var email = input.value;
            var regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            var errorMessage = document.getElementById('emailError');

            if (!regex.test(email)) {
                errorMessage.style.display = 'inline';
                input.style.borderColor = 'red';
            } else {
                errorMessage.style.display = 'none';
                input.style.borderColor = '';
            }
        }
    </script>

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="View and Update User Details"></asp:Label>
                    </h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">

                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_EmpCode" runat="server" AssociatedControlID="TB_EmpCode" Text="Employee Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_EmpCode" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_EmpCode" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_EmpCode" runat="server" ControlToValidate="TB_EmpCode" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_EmpCode" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Employee Code" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_EmpName" runat="server" AssociatedControlID="TB_EmpName" Text="Employee Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_EmpName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_EmpName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_EmpName" runat="server" ControlToValidate="TB_EmpName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z ]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_EmpName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Employee Name" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Company" runat="server" AssociatedControlID="DDL_Company" Text="Company Id" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Company" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="DDL_Company" ValidationGroup="Submit" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Company" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Region" runat="server" AssociatedControlID="DDL_Region" Text="Region " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Region" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="DDL_Region" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Region" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Division" runat="server" AssociatedControlID="DDL_Division" Text="Division  " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Division" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Division" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Division" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Department" runat="server" AssociatedControlID="DDL_Department" Text="Department " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Department" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Department" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Department" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_SubDepartment" runat="server" AssociatedControlID="DDL_SubDepartment" Text="Sub Department " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_SubDepartment" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_SubDepartment" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_SubDepartment" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Category" runat="server" AssociatedControlID="DDL_Category" Text=" Category " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Category" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Category" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Category" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Grade" runat="server" AssociatedControlID="DDL_Grade" Text="Grade " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Grade" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Grade" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Grade" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Branch" runat="server" AssociatedControlID="DDL_Branch" Text="Branch Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Branch" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Branch" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Branch" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DOJ" runat="server" AssociatedControlID="TB_DOJ" Text="DOJ :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DOJ" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_DOJ" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DOJ" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Email" runat="server" AssociatedControlID="TB_Email" Text="Email :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <span id="emailError" style="color: red; display: none;">Invalid email format</span>
                                    <%-- <asp:RequiredFieldValidator ID="RFV_TB_Email" runat="server" ErrorMessage="Email is required " ControlToValidate="TB_Email" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Email" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Email" oninput="validateEmail(this)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mobile" runat="server" AssociatedControlID="TB_Mobile" Text="Mobile :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Mobile" runat="server" ErrorMessage="Mobile is required " ControlToValidate="TB_Mobile" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Mobile" runat="server" ControlToValidate="TB_Mobile" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Not correct format (no. should start from 6-9)" ValidationExpression="^[6-9][0-9]{9}$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mobile" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Mobile no."></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_RBL_Gender" runat="server" AssociatedControlID="RBL_Gender" Text="Gender :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Gender" runat="server" ValidationGroup="" ErrorMessage="" ForeColor="Red" ControlToValidate="RBL_Gender" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Gender" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                            <asp:ListItem Text="Male" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Female" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Password" runat="server" AssociatedControlID="TB_Password" Text="Password :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Password" runat="server" ErrorMessage="Password is required" ControlToValidate="TB_Password" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red">
                                    </asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Password" runat="server" ControlToValidate="TB_Password" ValidationGroup="Submit" ForeColor="Red"
                                        ErrorMessage="Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character." ValidationExpression="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$"
                                        Display="Dynamic">
                                    </asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Password" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter your password"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <%--Button--%>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_BtnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSubmit" runat="server" Text="UPDATE" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="BtnSubmit_Click" />
                                        <asp:Button ID="BtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="BtnReset_Click" />
                                        <asp:Button ID="Btn_home" runat="server" Text="CANCEL" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/viewusers.aspx" />
                                    </div>
                                </div>
                            </div>

                        </div>

                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
