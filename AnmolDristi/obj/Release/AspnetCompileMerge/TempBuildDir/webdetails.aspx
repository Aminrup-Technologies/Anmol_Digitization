<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="webdetails.aspx.cs" Inherits="AnmolDristi.webdetails" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .is-invalid {
            border-color: #dc3545 !important;
            box-shadow: 0 0 0 0.2rem rgba(220,53,69,.25);
        }
    </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <%--<script type="text/javascript">
        function validateForm() {
            var siteName = document.getElementById('<%= txtSiteName.ClientID %>').value.trim();
            var siteURL = document.getElementById('<%= txtSiteURL.ClientID %>').value.trim();
            var purpose = document.getElementById('<%= txtPurpose.ClientID %>').value.trim();
            var frequency = document.getElementById('<%= ddlFrequency.ClientID %>').value;
            var loginID = document.getElementById('<%= txtLoginID.ClientID %>').value.trim();
            var password = document.getElementById('<%= txtPassword.ClientID %>').value.trim();
            var email = document.getElementById('<%= txtEmail.ClientID %>').value.trim();

            if (siteName === "" || purpose === "" || loginID === "" || password === "" || frequency === "") {
                new PNotify({
                    title: 'Validation Error',
                    text: 'Please fill all the required fields (marked in red).',
                    type: 'error',
                    styling: 'bootstrap3'
                });
                return false;
            }

            // Basic email format validation
            var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (email !== "" && !emailRegex.test(email)) {
                new PNotify({
                    title: 'Invalid Email',
                    text: 'Please enter a valid email address.',
                    type: 'warning',
                    styling: 'bootstrap3'
                });
                return false;
            }

            return true;
        }
    </script>--%>

    <script type="text/javascript">
        function validateForm() {
            var isValid = true;

            // Helper to highlight or reset field styles
            function markInvalid(control, highlight) {
                if (highlight) {
                    control.classList.add("is-invalid");
                } else {
                    control.classList.remove("is-invalid");
                }
            }

            var siteName = document.getElementById('<%= txtSiteName.ClientID %>');
            var siteURL = document.getElementById('<%= txtSiteURL.ClientID %>');
            var purpose = document.getElementById('<%= txtPurpose.ClientID %>');
            var frequency = document.getElementById('<%= ddlFrequency.ClientID %>');
            var loginID = document.getElementById('<%= txtLoginID.ClientID %>');
            var password = document.getElementById('<%= txtPassword.ClientID %>');
            var email = document.getElementById('<%= txtEmail.ClientID %>');

            // Validate required fields
            if (siteName.value.trim() === "") { markInvalid(siteName, true); isValid = false; } else { markInvalid(siteName, false); }
            if (purpose.value.trim() === "") { markInvalid(purpose, true); isValid = false; } else { markInvalid(purpose, false); }
            if (frequency.value === "") { markInvalid(frequency, true); isValid = false; } else { markInvalid(frequency, false); }
            if (loginID.value.trim() === "") { markInvalid(loginID, true); isValid = false; } else { markInvalid(loginID, false); }
            if (password.value.trim() === "") { markInvalid(password, true); isValid = false; } else { markInvalid(password, false); }

            // Optional email validation
            var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (email.value.trim() !== "" && !emailRegex.test(email.value)) {
                markInvalid(email, true);
                isValid = false;
                new PNotify({
                    title: 'Invalid Email',
                    text: 'Please enter a valid email address.',
                    type: 'warning',
                    styling: 'bootstrap3'
                });
            } else {
                markInvalid(email, false);
            }

            // Notify if validation fails
            if (!isValid) {
                new PNotify({
                    title: 'Validation Error',
                    text: 'Please correct the highlighted fields.',
                    type: 'error',
                    styling: 'bootstrap3'
                });
            }

            return isValid;
        }
    </script>


    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Manage Website Credentials</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content row">

                            <!-- Site Name -->
                            <div class="col-md-3">
                                <asp:Label ID="lblSiteName" runat="server" Text="Site Name" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtSiteName" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                                <asp:RequiredFieldValidator ID="rfvSiteName" runat="server" ControlToValidate="txtSiteName" ErrorMessage="*" ForeColor="Red" Display="Dynamic" />
                            </div>

                            <!-- Site URL -->
                            <div class="col-md-3">
                                <asp:Label ID="lblSiteURL" runat="server" Text="Site URL" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtSiteURL" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="255" />
                            </div>

                            <!-- Purpose -->
                            <div class="col-md-3">
                                <asp:Label ID="lblPurpose" runat="server" Text="Purpose" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtPurpose" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Frequency -->
                            <div class="col-md-3">
                                <asp:Label ID="lblFrequency" runat="server" Text="Usage Frequency" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:DropDownList ID="ddlFrequency" runat="server" CssClass="form-control form-control-sm rounded">
                                    <asp:ListItem Text="Select" Value="" />
                                    <asp:ListItem Text="Monthly" Value="Monthly" />
                                    <asp:ListItem Text="Quarterly" Value="Quarterly" />
                                    <asp:ListItem Text="Half-Yearly" Value="Half-Yearly" />
                                    <asp:ListItem Text="Yearly" Value="Yearly" />
                                </asp:DropDownList>
                            </div>

                            <!-- Login ID -->
                            <div class="col-md-3">
                                <asp:Label ID="lblLoginID" runat="server" Text="Login ID" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtLoginID" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Password -->
                            <div class="col-md-3">
                                <asp:Label ID="lblPassword" runat="server" Text="Password" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Registered Mobile -->
                            <div class="col-md-3">
                                <asp:Label ID="lblMobile" runat="server" Text="Registered Mobile" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="15" />
                            </div>

                            <!-- Registered Email -->
                            <div class="col-md-3">
                                <asp:Label ID="lblEmail" runat="server" Text="Registered Email" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- OTP Required -->
                            <div class="col-md-3">
                                <asp:Label ID="lblOTP" runat="server" Text="OTP Required?" CssClass="text-primary font-weight-bold small"></asp:Label><br />
                                <asp:CheckBox ID="chkOTPRequired" runat="server" />
                            </div>

                            <!-- Notes -->
                            <div class="col-md-3">
                                <asp:Label ID="lblNotes" runat="server" Text="Notes" CssClass="text-primary font-weight-bold small"></asp:Label>
                                <asp:TextBox ID="txtNotes" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="255" />
                            </div>

                            <!-- Buttons -->
                            <div class="col-md-6 mt-3">
                                <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" ValidationGroup="Submit" OnClientClick="return validateForm();" OnClick="btnSubmit_Click" />
                                <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btnReset_Click" />
                                <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btnCancel_Click" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- GridView -->
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Saved Website Credentials</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:GridView ID="GridViewCredentials" runat="server" AutoGenerateColumns="false"
                                CssClass="table table-bordered table-hover table-sm" DataKeyNames="Id" AllowPaging="true" PageSize="20" OnRowCommand="GridViewCredentials_RowCommand" OnRowDeleting="GridViewCredentials_RowDeleting" EmptyDataText="No credentials found.">

                                <Columns>
                                    <asp:BoundField DataField="SiteName" HeaderText="Site Name" />
                                    <asp:BoundField DataField="SiteURL" HeaderText="URL" />
                                    <asp:BoundField DataField="Purpose" HeaderText="Purpose" />
                                    <asp:BoundField DataField="LoginId" HeaderText="Login ID" />
                                    <asp:BoundField DataField="RegisteredEmail" HeaderText="Email" />
                                    <asp:BoundField DataField="RegisteredMobile" HeaderText="Mobile" />
                                    <asp:TemplateField HeaderText="OTP">
                                        <ItemTemplate>
                                            <asp:CheckBox ID="chkOTPView" runat="server" Checked='<%# Convert.ToBoolean(Eval("IsOTPRequired")) %>' Enabled="false" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Edit">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lnkEdit" runat="server" CommandName="EditRecord" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-sm btn-primary">Edit</asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:CommandField ShowDeleteButton="True" ButtonType="Button" HeaderText="Delete" ControlStyle-CssClass="btn btn-sm btn-danger" />
                                </Columns>
                            </asp:GridView>

                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
