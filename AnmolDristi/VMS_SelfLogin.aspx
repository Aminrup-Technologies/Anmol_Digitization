<%@ Page Title="Visitor Login" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_SelfLogin.aspx.cs" Inherits="AnmolDristi.VMS_SelfLogin" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .mobile-card {
            background: #fff;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
            margin-top: 50px;
        }

        .btn-mobile {
            width: 100%;
            border-radius: 25px;
            padding: 10px;
            font-size: 16px;
            font-weight: bold;
            margin-top: 10px;
        }

        .brand-text {
            color: #1ABB9C;
            font-weight: 800;
            margin-bottom: 20px;
        }

        .otp-box {
            text-align: center;
            letter-spacing: 5px;
            font-size: 18px;
            font-weight: bold;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main" style="background-color: #f7f7f7; min-height: 100vh;">
        <div class="row">
            <div class="col-md-4 col-md-offset-4 col-sm-6 col-sm-offset-3 col-xs-12">

                <div class="mobile-card text-center">
                    
                    <asp:Image ID="imgLogo" runat="server" ImageUrl="~/WebData/anmol_logo_GPTW.jpg" style="max-height: 80px; margin-bottom: 15px;" AlternateText="Anmol Logo" />
                    
                    <h2 class="brand-text">Anmol VMS</h2>
                    
                    <h4>Welcome to <span style="color:#73879C;"><asp:Label ID="lblBranchName" runat="server" Text="our Facility"></asp:Label></span></h4>
                    <p class="text-muted">Please verify your contact details to proceed.</p>
                    
                    <div class="ln_solid"></div>
                    <asp:HiddenField ID="hfBranchId" runat="server" />
                    
                    <asp:Panel ID="pnlRequestOTP" runat="server">
                        <div class="form-group text-left">
                            <label>Mobile Number *</label>
                            <div class="input-group">
                                <span class="input-group-addon">+91</span>
                                <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control" MaxLength="10" placeholder="10-digit mobile number" TextMode="Number"></asp:TextBox>
                            </div>
                        </div>
                        <asp:Button ID="btnSendOTP" runat="server" CssClass="btn btn-success btn-mobile" Text="Send Verification Code" OnClick="btnSendOTP_Click" />
                    </asp:Panel>

                    <asp:Panel ID="pnlVerifyOTP" runat="server" Visible="false">
                        <div class="alert alert-info" style="font-size: 12px; padding: 10px;">
                            Code sent to:<br />
                            <strong><asp:Label ID="lblDisplayMobile" runat="server"></asp:Label></strong>
                            <asp:Label ID="lblDisplayEmail" runat="server" Visible="false"></asp:Label>
                        </div>

                        <div class="form-group text-left">
                            <label>Verification Code *</label>
                            <asp:TextBox ID="txtMobileOTP" runat="server" CssClass="form-control otp-box" MaxLength="6" placeholder="• • • • • •" TextMode="Number"></asp:TextBox>
                        </div>

                        <%--<div class="form-group text-left">
                            <label>Email OTP *</label>
                            <asp:TextBox ID="txtEmailOTP" runat="server" CssClass="form-control otp-box" MaxLength="6" placeholder="• • • • • •" TextMode="Number"></asp:TextBox>
                        </div>--%>

                        <asp:Panel ID="pnlEmailOTPInput" runat="server" CssClass="form-group text-left">
                            <label>Email OTP</label>
                            <asp:TextBox ID="txtEmailOTP" runat="server" CssClass="form-control otp-box" MaxLength="6" placeholder="• • • • • •" TextMode="Number"></asp:TextBox>
                        </asp:Panel>

                        <asp:Button ID="btnVerifyOTP" runat="server" CssClass="btn btn-primary btn-mobile" Text="Verify & Proceed" OnClick="btnVerifyOTP_Click" />

                        <div style="margin-top: 15px;">
                            <asp:LinkButton ID="lnkResend" runat="server" OnClick="lnkResend_Click" CssClass="text-muted">Didn't receive it? Resend Code</asp:LinkButton>
                        </div>
                    </asp:Panel>

                    <div style="margin-top: 20px;">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"></asp:Label>
                    </div>

                </div>

            </div>
        </div>
    </div>
</asp:Content>