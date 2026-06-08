<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="AnmolDristi.login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <title>Apps - Anmol Industries</title>
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
    <link rel="icon" type="image/x-icon" href="WebData/Anmol_Logo.png" />
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="Resources/bootstrap/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="Resources/font-awesome/css/font-awesome.min.css" rel="stylesheet" />
    <link href="Resources/custom/custom.min.css" rel="stylesheet" />
    <script type="text/javascript">
        try {
            sessionStorage.removeItem('ActiveLoginToken');
            sessionStorage.removeItem('ActiveSessionId');
        } catch (e) { }
    </script>
</head>
<body class="login">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

        <asp:UpdatePanel ID="upLogin" runat="server">
            <ContentTemplate>
                <div class="login_wrapper">
                    <div class="login_form">
                        <section class="login_content">
                            <asp:Image ID="Img_CpLogo" runat="server" ImageUrl="#" Height="120" Width="180" />
                            <h1>
                                <asp:Label ID="lbl_companyname" runat="server" Text="NA"></asp:Label></h1>

                            <asp:Panel ID="pnlLogin" runat="server">
                                <div class="form-horizontal">
                                    <div class="form-group row">
                                        <asp:Label ID="Label1" runat="server" AssociatedControlID="txt_loginid" CssClass="control-label col-md-4 col-sm-6 label-align" Font-Bold="true" Text="LOGIN ID/ EMP CODE"></asp:Label>
                                        <div class="col-md-8 col-sm-6">
                                            <asp:TextBox ID="txt_loginid" runat="server" CssClass="form-control" placeholder="AHO__"></asp:TextBox>
                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="txt_loginid" ValidationGroup="vgLogin" Display="Dynamic"></asp:RequiredFieldValidator>
                                        </div>
                                    </div>
                                    <div class="form-group row">
                                        <asp:Label ID="Label2" runat="server" AssociatedControlID="txt_password" CssClass="control-label col-md-4 col-sm-6 label-align" Font-Bold="true" Text="Password"></asp:Label>
                                        <div class="col-md-8 col-sm-6 ">
                                            <asp:TextBox ID="txt_password" runat="server" class="form-control" placeholder="Enter your Password" TextMode="Password"></asp:TextBox>
                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="txt_password" ValidationGroup="vgLogin" Display="Dynamic"></asp:RequiredFieldValidator>
                                        </div>
                                    </div>
                                    <div>
                                        <asp:Button ID="btn_signin" runat="server" Text="Login" class="btn btn-success submit" ValidationGroup="vgLogin" OnClick="btn_signin_Click" />
                                    </div>
                                    <div class="clearfix"></div>
                                    <div class="separator mt-3">
                                        <asp:LinkButton ID="lnkForgotPassword" runat="server" OnClick="lnkForgotPassword_Click" CausesValidation="false" Font-Bold="true" ForeColor="#007bff">Forgot Password?</asp:LinkButton>
                                    </div>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlRequestOTP" runat="server" Visible="false">
                                <h4>Account Setup / Recovery</h4>
                                <p>Enter your Employee Code to receive an OTP.</p>
                                <div class="form-group row">
                                    <div class="col-md-12">
                                        <asp:TextBox ID="txtResetEmpCode" runat="server" CssClass="form-control" placeholder="Enter Employee Code (e.g. AHO__)"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="reqResetEmpCode" runat="server" ControlToValidate="txtResetEmpCode" ErrorMessage="Required." ForeColor="Red" ValidationGroup="vgOtpRequest" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>
                                </div>
                                <div>
                                    <asp:Button ID="btnSendOTP" runat="server" Text="Send OTP" CssClass="btn btn-primary" ValidationGroup="vgOtpRequest" OnClick="btnSendOTP_Click" />
                                    <asp:LinkButton ID="lnkBackToLogin1" runat="server" OnClick="lnkBackToLogin_Click" CssClass="btn btn-secondary" CausesValidation="false">Back</asp:LinkButton>
                                </div>
                                <div class="mt-3">
                                    <asp:LinkButton ID="lnkNeedToUpdateContact" runat="server" OnClick="lnkNeedToUpdateContact_Click" CausesValidation="false" Font-Size="Smaller" ForeColor="#dc3545">First Time Login</asp:LinkButton>
                                </div>
                            </asp:Panel>

                            <!-- PANEL 2.5: Self-Verification & Contact Update -->
                            <asp:Panel ID="pnlSelfVerify" runat="server" Visible="false">
                                <h4>Verify Identity & Update Contact</h4>
                                <p class="text-danger small">To update your mobile/email, please verify your internal HR details.</p>

                                <div class="form-group row">
                                    <!-- Employee Code with AutoPostBack -->
                                    <div class="col-md-12 mb-2">
                                        <asp:TextBox ID="txtVerifyEmpCode" runat="server" CssClass="form-control" placeholder="Your Employee Code" AutoPostBack="true" OnTextChanged="txtVerifyEmpCode_TextChanged"></asp:TextBox>
                                        <asp:Label ID="lblFetchedEmpName" runat="server" CssClass="small mt-1 d-block"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server" ControlToValidate="txtVerifyEmpCode" ErrorMessage="Required" ForeColor="Red" ValidationGroup="vgSelfVerify" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>

                                    <!-- Manager Code with AutoPostBack -->
                                    <div class="col-md-12 mb-2">
                                        <asp:TextBox ID="txtVerifyManagerCode" runat="server" CssClass="form-control" placeholder="Your Reporting Manager Code (e.g. AHO___)" AutoPostBack="true" OnTextChanged="txtVerifyManagerCode_TextChanged"></asp:TextBox>
                                        <asp:Label ID="lblFetchedManagerName" runat="server" CssClass="small mt-1 d-block"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator4" runat="server" ControlToValidate="txtVerifyManagerCode" ErrorMessage="Required" ForeColor="Red" ValidationGroup="vgSelfVerify" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>

                                    <!-- Changed to Date of Joining (DOJ) -->
                                    <div class="col-md-12 mb-4">
                                        <label class="small text-muted mb-0">Date of Joining (YYYY-MM-DD)</label>
                                        <asp:TextBox ID="txtVerifyDOB" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator5" runat="server" ControlToValidate="txtVerifyDOB" ErrorMessage="Required" ForeColor="Red" ValidationGroup="vgSelfVerify" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>

                                    <h5 class="col-md-12 border-top pt-2">New Contact Details</h5>
                                    <div class="col-md-12 mb-2">
                                        <asp:TextBox ID="txtNewMobile" runat="server" CssClass="form-control" placeholder="New 10-digit Mobile No" MaxLength="10"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator6" runat="server" ControlToValidate="txtNewMobile" ErrorMessage="Required" ForeColor="Red" ValidationGroup="vgSelfVerify" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>
                                    <div class="col-md-12 mb-2">
                                        <asp:TextBox ID="txtNewEmail" runat="server" CssClass="form-control" placeholder="New Email Address" TextMode="Email"></asp:TextBox>
                                    </div>

                                    <!-- NEW: DPDP Consent Checkbox (Visually Aligned) -->
                                    <div class="col-md-12 mb-3 mt-2">
                                        <div class="p-3" style="background-color: #f8f9fa; border-radius: 5px; border: 1px solid #dee2e6;">
                                            <div class="d-flex align-items-start">
                                                <asp:CheckBox ID="chkDPDPConsent" runat="server" CssClass="mt-1 mr-2" />
                                                <label for="<%= chkDPDPConsent.ClientID %>" class="small text-muted font-weight-bold mb-0" style="cursor: pointer;">
                                                    I explicitly consent to the collection, storage, and processing of my personal mobile number and email address for the purpose of system authentication and official communications in compliance with the DPDP Act.
                                                </label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div>
                                    <asp:Button ID="btnVerifyAndUpdate" runat="server" Text="Update & Send OTP" CssClass="btn btn-warning" ValidationGroup="vgSelfVerify" OnClick="btnVerifyAndUpdate_Click" />
                                    <asp:LinkButton ID="lnkBackToLogin3" runat="server" OnClick="lnkBackToLogin_Click" CssClass="btn btn-secondary" CausesValidation="false">Cancel</asp:LinkButton>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlVerifyOTP" runat="server" Visible="false">
                                <h4>Verify Identity</h4>
                                <p class="text-success">
                                    <asp:Label ID="lblMaskedContact" runat="server"></asp:Label>
                                </p>
                                <div class="form-group row">
                                    <div class="col-md-12">
                                        <asp:TextBox ID="txtOTP" runat="server" CssClass="form-control" placeholder="Enter 6-digit OTP" MaxLength="6"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="reqOTP" runat="server" ControlToValidate="txtOTP" ErrorMessage="OTP is required." ForeColor="Red" ValidationGroup="vgOtpVerify" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>
                                </div>
                                <div>
                                    <asp:Button ID="btnVerifyOTP" runat="server" Text="Verify OTP" CssClass="btn btn-success" ValidationGroup="vgOtpVerify" OnClick="btnVerifyOTP_Click" />
                                    <asp:LinkButton ID="lnkBackToLogin2" runat="server" OnClick="lnkBackToLogin_Click" CssClass="btn btn-secondary" CausesValidation="false">Cancel</asp:LinkButton>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlSetPassword" runat="server" Visible="false">
                                <h4>Set New Password</h4>
                                <div class="form-group row">
                                    <div class="col-md-12 mb-2">
                                        <asp:TextBox ID="txtNewPassword" runat="server" CssClass="form-control" placeholder="New Password" TextMode="Password"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="reqNewPass" runat="server" ControlToValidate="txtNewPassword" ErrorMessage="Required" ForeColor="Red" ValidationGroup="vgSetPass" Display="Dynamic"></asp:RequiredFieldValidator>
                                    </div>
                                    <div class="col-md-12">
                                        <asp:TextBox ID="txtConfirmPassword" runat="server" CssClass="form-control" placeholder="Confirm Password" TextMode="Password"></asp:TextBox>
                                        <asp:CompareValidator ID="cmpPass" runat="server" ControlToCompare="txtNewPassword" ControlToValidate="txtConfirmPassword" ErrorMessage="Passwords do not match." ForeColor="Red" ValidationGroup="vgSetPass" Display="Dynamic"></asp:CompareValidator>
                                    </div>
                                </div>
                                <div>
                                    <asp:Button ID="btnSavePassword" runat="server" Text="Save & Login" CssClass="btn btn-primary" ValidationGroup="vgSetPass" OnClick="btnSavePassword_Click" />
                                </div>
                            </asp:Panel>

                            <div class="separator mt-4">
                                <p>
                                    ©
                                    <asp:Label ID="lbl_currentyr" runat="server"></asp:Label>
                                    All Rights Reserved.
                                    <br />
                                    <span style="font-weight: bold; color: red;">
                                        <asp:Label ID="lbl_compfooter" runat="server"></asp:Label></span><br />
                                    Powered by
                                    <asp:Label ID="lbl_owner" runat="server"></asp:Label>
                                </p>
                            </div>
                        </section>
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>

    </form>
</body>
</html>
