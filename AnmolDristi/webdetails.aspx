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

    <%--<script type="text/javascript">
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
    </script>--%>

    <script type="text/javascript">
        function validateForm() {
            var isValid = true;

            function markInvalid(control, highlight) {
                if (control) {
                    if (highlight) {
                        control.classList.add("is-invalid");
                    } else {
                        control.classList.remove("is-invalid");
                    }
                }
            }

            // Helper to get element and warn if missing
            function getEl(id) {
                var el = document.getElementById(id);
                if (!el) {
                    console.warn("Element not found: " + id);
                }
                return el;
            }

            var siteName = getEl('<%= txtSiteName.ClientID %>');
        var siteURL = getEl('<%= txtSiteURL.ClientID %>');
        var purpose = getEl('<%= txtPurpose.ClientID %>');
        var frequency = getEl('<%= ddlFrequency.ClientID %>');
        var loginID = getEl('<%= txtLoginID.ClientID %>');
        var password = getEl('<%= txtPassword.ClientID %>');
        var mobile = getEl('<%= txtMobile.ClientID %>');
        var mobileOwner = getEl('<%= txtMobileOwnerName.ClientID %>');
        var email = getEl('<%= txtEmail.ClientID %>');
        var emailOwner = getEl('<%= txtEmailOwnerName.ClientID %>');
        var otpRequired = getEl('<%= chkOTPRequired.ClientID %>');
        var otpType = getEl('<%= ddlOTPType.ClientID %>');
        var officeLocation = getEl('<%= DDL_Branch.ClientID %>');
        var department = getEl('<%= DDL_Department.ClientID %>');

        // Begin validations
        if (siteName && siteName.value.trim() === "") { markInvalid(siteName, true); isValid = false; }
        else { markInvalid(siteName, false); }

        if (purpose && purpose.value.trim() === "") { markInvalid(purpose, true); isValid = false; }
        else { markInvalid(purpose, false); }

        if (frequency && frequency.value === "") { markInvalid(frequency, true); isValid = false; }
        else { markInvalid(frequency, false); }

        if (loginID && loginID.value.trim() === "") { markInvalid(loginID, true); isValid = false; }
        else { markInvalid(loginID, false); }

        if (password && password.value.trim() === "") { markInvalid(password, true); isValid = false; }
        else { markInvalid(password, false); }

        // Mobile validations
        var mobileRegex = /^\d{10}$/;
        if (mobile && mobile.value.trim() !== "") {
            if (!mobileRegex.test(mobile.value.trim())) {
                markInvalid(mobile, true);
                isValid = false;
                notify("Invalid Mobile Number", "Please enter a valid 10-digit mobile number.");
            } else {
                markInvalid(mobile, false);
            }
            if (mobileOwner && mobileOwner.value.trim() === "") {
                markInvalid(mobileOwner, true); isValid = false;
            } else {
                markInvalid(mobileOwner, false);
            }
        }

        // Email validations
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (email && email.value.trim() !== "") {
            if (!emailRegex.test(email.value.trim())) {
                markInvalid(email, true);
                isValid = false;
                notify("Invalid Email", "Please enter a valid email address.");
            } else {
                markInvalid(email, false);
            }
            if (emailOwner && emailOwner.value.trim() === "") {
                markInvalid(emailOwner, true); isValid = false;
            } else {
                markInvalid(emailOwner, false);
            }
        }

        if (otpRequired && otpRequired.checked) {
            if (otpType && otpType.value === "") {
                markInvalid(otpType, true); isValid = false;
            } else {
                markInvalid(otpType, false);
            }
        }

        if (officeLocation && officeLocation.value === "") {
            markInvalid(officeLocation, true); isValid = false;
        } else {
            markInvalid(officeLocation, false);
        }

        if (department && department.value === "") {
            markInvalid(department, true); isValid = false;
        } else {
            markInvalid(department, false);
        }

        if (!isValid) {
            notify("Validation Error", "Please correct the highlighted fields.");
        }

        return isValid;
    }

    // Unified notifier
    function notify(title, message) {
        new PNotify({
            title: title,
            text: message,
            type: 'warning',
            styling: 'bootstrap3'
        });
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
                                <asp:Label ID="lblSiteName" runat="server" Text="Site Name <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtSiteName" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Site URL -->
                            <div class="col-md-3">
                                <asp:Label ID="lblSiteURL" runat="server" Text="Site URL <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtSiteURL" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="255" />
                            </div>

                            <!-- Purpose -->
                            <div class="col-md-3">
                                <asp:Label ID="lblPurpose" runat="server" Text="Purpose <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtPurpose" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Frequency -->
                            <div class="col-md-3">
                                <asp:Label ID="lblFrequency" runat="server" Text="Usage Frequency <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
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
                                <asp:Label ID="lblLoginID" runat="server" Text="Login ID <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtLoginID" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Password -->
                            <div class="col-md-3">
                                <asp:Label ID="lblPassword" runat="server" Text="Password <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Registered Mobile -->
                            <div class="col-md-3">
                                <asp:Label ID="lblMobile" runat="server" Text="Registered Mobile No. <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Number" MaxLength="10" />
                            </div>

                            <!-- Registered Mobile Owner Name -->
                            <div class="col-md-3">
                                <asp:Label ID="lblMobileOwnerName" runat="server" Text="Registered Person Name for Mobile OTP <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtMobileOwnerName" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Registered Email -->
                            <div class="col-md-3">
                                <asp:Label ID="lblEmail" runat="server" Text="Registered Email ID <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- Registered Email Owner Name -->
                            <div class="col-md-3">
                                <asp:Label ID="lblEmailOwnerName" runat="server" Text="Registered Person Name for Email OTP <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:TextBox ID="txtEmailOwnerName" runat="server" CssClass="form-control form-control-sm rounded" MaxLength="100" />
                            </div>

                            <!-- OTP Required -->
                            <div class="col-md-3">
                                <asp:Label ID="lblOTP" runat="server" Text="OTP Required? <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" /><br />
                                <asp:CheckBox ID="chkOTPRequired" runat="server" />&nbsp;Yes
                            </div>

                            <!-- OTP Type -->
                            <div class="col-md-3">
                                <asp:Label ID="lblOTPType" runat="server" Text="OTP Type <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:DropDownList ID="ddlOTPType" runat="server" CssClass="form-control form-control-sm rounded">
                                    <asp:ListItem Text="Select" Value="" />
                                    <asp:ListItem Text="Mobile OTP" Value="Mobile" />
                                    <asp:ListItem Text="Email OTP" Value="Email" />
                                    <asp:ListItem Text="Both" Value="Both" />
                                </asp:DropDownList>
                            </div>

                            <!-- Office Location -->
                            <div class="col-md-3">
                                <asp:Label ID="lblOfficeLocation" runat="server" Text="Office Location <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:DropDownList ID="DDL_Branch" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                            </div>

                            <!-- Department -->
                            <div class="col-md-3">
                                <asp:Label ID="lblDepartment" runat="server" Text="Department <span class='text-danger'>*</span>" CssClass="text-primary font-weight-bold small" />
                                <asp:DropDownList ID="DDL_Department" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                            </div>

                            <!-- Notes -->
                            <div class="col-md-3">
                                <asp:Label ID="lblNotes" runat="server" Text="Remarks / Notes" CssClass="text-primary font-weight-bold small" />
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
                            <asp:GridView ID="GridViewCredentials" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered table-hover table-sm" DataKeyNames="Id" AllowPaging="true" PageSize="20" OnRowCommand="GridViewCredentials_RowCommand" OnRowDeleting="GridViewCredentials_RowDeleting" OnPageIndexChanging="GridViewCredentials_PageIndexChanging" EmptyDataText="No credentials found.">

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
