<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qci_analtics.aspx.cs" Inherits="AnmolDristi.qaqc_qci_analtics" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_content">
                            <%--<iframe src="https://reports.aminruptechnologies.co.in/superset/dashboard/p/Z94vygY5WlY/" style="width: 100%; height: 800px; border: none;"></iframe>--%>
                            <iframe src="https://reports.aminruptechnologies.co.in/superset/dashboard/p/Z94vygY5WlY/" style="width: 100%; height:800px; border: none;" sandbox="allow-scripts allow-forms allow-same-origin"></iframe>
                            <%--<iframe width="100%" seamless frameborder="1" src="https://reports.aminruptechnologies.co.in/superset/dashboard/p/Z94vygY5WlY/?standalone=1"></iframe>--%>

                        </div>

                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
