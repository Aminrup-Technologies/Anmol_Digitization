<%@ Page Title="Anmol Industries | Home" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="home.aspx.cs" Inherits="AnmolDristi.home" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-lg-12 profile_details">
                <div class="well profile_view col-sm-12 col-lg-12">
                    <div class="col-sm-12">
                        <h4 class="brief"><b>Employee Card</b> :
							<asp:Label ID="lbl_empcode" runat="server" Text="N/A"></asp:Label>
                        </h4>

                        <div class="right col-md-6 col-sm-4 text-center">
                            <img id="ProfilePic_3" runat="server" src="~/WebData/No_Image.jpg" width="200" height="260" alt="ProfilePhoto" class="img-circle img-fluid small">
                        </div>

                        <div class="left col-md-6 col-sm-8 text-left">
                            <h2 style="text-align: center;">
                                <asp:Label ID="lbl_workmansl" runat="server" Text="N/A" ForeColor="DarkBlue" Font-Bold="true"></asp:Label>
                                :
								<asp:Label ID="lbl_username" runat="server" Text="N/A" ForeColor="Black" Font-Bold="true"></asp:Label>
                            </h2>
                            <hr />
                            <p>
                                <strong>Designation : </strong>
                                <asp:Label ID="lbl_desg" runat="server" Text="N/A"></asp:Label>
                                [<asp:Label ID="lbl_skillcat" runat="server" Text="N/A"></asp:Label>]
                            </p>

                            <p>
                                <strong>Deputed Location :</strong>
                                <asp:Label ID="lbl_wrkcopmany" runat="server" Text="N/A"></asp:Label>,
								<asp:Label ID="lbl_region" runat="server" Text="N/A"></asp:Label>,
								<asp:Label ID="lbl_state" runat="server" Text="N/A"></asp:Label>.
                            </p>

                            <p>
                                <strong>Deputed Site :</strong>
                                <asp:Label ID="lbl_wrksite" runat="server" Text="N/A"></asp:Label>
                            </p>

                            <ul class="list-unstyled">
                                <li><i class="fa fa-calendar">&nbsp;</i><strong>DOJ :</strong> :
									<asp:Label ID="lbl_doj" runat="server" Text="N/A"></asp:Label></li>
                                <li><i class="fa fa-clock-o">&nbsp;</i>Work Tenure :
									<asp:Label ID="lbl_workage" runat="server" Text="N/A"></asp:Label></li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
