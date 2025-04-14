<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="AnmolDristi.login" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <title>Quality Reporting - Anmol Industries</title>
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
    <link rel="icon" type="image/x-icon" href="WebData/Anmol_Logo.png" />
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <!-- Bootstrap -->
    <link href="Resources/bootstrap/dist/css/bootstrap.min.css" rel="stylesheet" />
    <!-- Font Awesome -->
    <link href="Resources/font-awesome/css/font-awesome.min.css" rel="stylesheet" />
    <!-- NProgress -->
    <link href="Resources/nprogress/nprogress.css" rel="stylesheet" />
    <!-- Animate.css -->
    <link href="Resources/animate.css/animate.min.css" rel="stylesheet" />
    <!-- Custom Theme Style -->
    <link href="Resources/custom/custom.min.css" rel="stylesheet" />
</head>
<body class="login">
    <form id="form1" runat="server">
        <div>
            <div class="login_wrapper">
                <div class="login_form">
                    <section class="login_content">
                        <asp:Image ID="Img_CpLogo" runat="server" ImageUrl="#" Height="120" Width="180" />
                        <h1>
                            <asp:Label ID="lbl_companyname" runat="server" Text="Automation & Technical Services"></asp:Label></h1>
                        <div class="form-horizontal">
                            <div class="form-group row">
                                <asp:Label ID="Label1" runat="server" AssociatedControlID="txt_loginid" CssClass="control-label col-md-4 col-sm-6 label-align" Font-Bold="true" Text="USER ID"></asp:Label>
                                <div class="col-md-8 col-sm-6">
                                    <asp:TextBox ID="txt_loginid" runat="server" CssClass="form-control" placeholder="AHO__"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="txt_loginid" SetFocusOnError="true" Display="Dynamic"></asp:RequiredFieldValidator>
                                </div>
                            </div>

                            <div class="form-group row">
                                <asp:Label ID="Label2" runat="server" AssociatedControlID="txt_password" CssClass="control-label col-md-4 col-sm-6 label-align" Font-Bold="true" Text="Password"></asp:Label>
                                <div class="col-md-8 col-sm-6 ">
                                    <asp:TextBox ID="txt_password" runat="server" class="form-control" placeholder="Enter your Password" TextMode="Password"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="txt_password" SetFocusOnError="true" Display="Dynamic"></asp:RequiredFieldValidator>
                                </div>
                            </div>
                            <div>
                                <div class="clearfix"></div>
                                <asp:Button ID="btn_signin" runat="server" Text="Login" class="btn btn-success submit" OnClick="btn_signin_Click" />
                            </div>

                            <div class="clearfix"></div>

                            <div class="separator">
                                <br />
                                <div>
                                    <p>
                                        © 2024-<asp:Label ID="lbl_currentyr" runat="server" Text="2024"></asp:Label>
                                        All Rights Reserved. <span style="font-weight: bold; color: red;">
                                            <a href="https://www.anmolindustries.com/" target="_blank">
                                                <asp:Label ID="lbl_compfooter" runat="server" Text="N/A"></asp:Label></a><br />
                                        </span>Powered by <a href="#" target="_blank">
                                            <asp:Label ID="lbl_owner" runat="server" Text="N/A"></asp:Label></a>
                                    </p>
                                </div>
                            </div>
                        </div>
                    </section>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
