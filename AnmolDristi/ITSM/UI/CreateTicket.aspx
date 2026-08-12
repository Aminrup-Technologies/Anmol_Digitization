<%@ Page Title="ITSM | Create Ticket" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true"
    CodeBehind="CreateTicket.aspx.cs" Inherits="AnmolDristi.ITSM.UI.CreateTicket" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2><i class="fa fa-ticket"></i> Create ITSM Ticket</h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true" Style="display: block; margin-bottom: 15px;"></asp:Label>

                        <div class="row">
                            <div class="col-md-4 col-sm-6 col-xs-12 form-group">
                                <label>Request Type <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="ddlRequestType" runat="server" CssClass="form-control"
                                    AppendDataBoundItems="true">
                                    <asp:ListItem Text="-- Select Request Type --" Value="" />
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvRequestType" runat="server"
                                    ControlToValidate="ddlRequestType" InitialValue=""
                                    ErrorMessage="Request Type is required." Display="Dynamic"
                                    ForeColor="Red" ValidationGroup="CreateTicket" />
                            </div>

                            <div class="col-md-4 col-sm-6 col-xs-12 form-group">
                                <label>Category <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="ddlCategory" runat="server" CssClass="form-control"
                                    AutoPostBack="true" AppendDataBoundItems="true"
                                    OnSelectedIndexChanged="ddlCategory_SelectedIndexChanged">
                                    <asp:ListItem Text="-- Select Category --" Value="" />
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvCategory" runat="server"
                                    ControlToValidate="ddlCategory" InitialValue=""
                                    ErrorMessage="Category is required." Display="Dynamic"
                                    ForeColor="Red" ValidationGroup="CreateTicket" />
                            </div>

                            <div class="col-md-4 col-sm-6 col-xs-12 form-group">
                                <label>Sub Category <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="ddlSubCategory" runat="server" CssClass="form-control"
                                    AutoPostBack="true" AppendDataBoundItems="true"
                                    OnSelectedIndexChanged="ddlSubCategory_SelectedIndexChanged">
                                    <asp:ListItem Text="-- Select Sub Category --" Value="" />
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvSubCategory" runat="server"
                                    ControlToValidate="ddlSubCategory" InitialValue=""
                                    ErrorMessage="Sub Category is required." Display="Dynamic"
                                    ForeColor="Red" ValidationGroup="CreateTicket" />
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4 col-sm-6 col-xs-12 form-group">
                                <label>Item</label>
                                <asp:DropDownList ID="ddlItem" runat="server" CssClass="form-control"
                                    AppendDataBoundItems="true">
                                    <asp:ListItem Text="-- Select Item (optional) --" Value="" />
                                </asp:DropDownList>
                            </div>

                            <div class="col-md-4 col-sm-6 col-xs-12 form-group">
                                <label>Impact <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="ddlImpact" runat="server" CssClass="form-control"
                                    AppendDataBoundItems="true">
                                    <asp:ListItem Text="-- Select Impact --" Value="" />
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvImpact" runat="server"
                                    ControlToValidate="ddlImpact" InitialValue=""
                                    ErrorMessage="Impact is required." Display="Dynamic"
                                    ForeColor="Red" ValidationGroup="CreateTicket" />
                            </div>

                            <div class="col-md-4 col-sm-6 col-xs-12 form-group">
                                <label>Urgency <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="ddlUrgency" runat="server" CssClass="form-control"
                                    AppendDataBoundItems="true">
                                    <asp:ListItem Text="-- Select Urgency --" Value="" />
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvUrgency" runat="server"
                                    ControlToValidate="ddlUrgency" InitialValue=""
                                    ErrorMessage="Urgency is required." Display="Dynamic"
                                    ForeColor="Red" ValidationGroup="CreateTicket" />
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-12 col-sm-12 col-xs-12 form-group">
                                <label>Title <span class="text-danger">*</span></label>
                                <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control"
                                    MaxLength="200" placeholder="Short summary of the issue"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="rfvTitle" runat="server"
                                    ControlToValidate="txtTitle"
                                    ErrorMessage="Title is required." Display="Dynamic"
                                    ForeColor="Red" ValidationGroup="CreateTicket" />
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-12 col-sm-12 col-xs-12 form-group">
                                <label>Description</label>
                                <asp:TextBox ID="txtDescription" runat="server" CssClass="form-control"
                                    TextMode="MultiLine" Rows="5"
                                    placeholder="Describe the issue, steps to reproduce, and impact"></asp:TextBox>
                            </div>
                        </div>

                        <div class="ln_solid"></div>
                        <div class="form-group">
                            <div class="col-md-12">
                                <asp:Button ID="btnSubmit" runat="server" Text="Submit"
                                    CssClass="btn btn-success"
                                    ValidationGroup="CreateTicket"
                                    OnClick="btnSubmit_Click" />
                                <asp:Button ID="btnReset" runat="server" Text="Reset"
                                    CssClass="btn btn-default"
                                    CausesValidation="false"
                                    OnClick="btnReset_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
