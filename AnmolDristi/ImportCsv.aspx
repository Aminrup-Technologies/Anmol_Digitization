<%@ Page Title="Anmol | Import SO Travel Register" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ImportCsv.aspx.cs" Inherits="AnmolDristi.ImportCsv" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Main Heading</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12  ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Sub Heading</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Select & Upload .csv File <span class="text text-danger">*</span></label>
                                        <asp:FileUpload ID="fuCsv" CssClass="form-control form-control-sm rounded" runat="server" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6 center-margin">
                                <div class="ln_solid"></div>
                                <div class="item form-group row">
                                    <div class="col-md-6 col-sm-12">
                                        <asp:Label ID="lblMessage" runat="server" Text="Click SUBMIT to Save Data!!"></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12">
                                        <button type="button" class="btn btn-danger btn-sm collapse-link">Cancel</button>
                                        <button type="reset" class="btn btn-warning btn-sm">Reset</button>
                                        <asp:Button ID="btnUpload" runat="server" Text="Import CSV" CssClass="btn btn-success btn-sm" OnClick="btnUpload_Click" />
                                    </div>
                                </div>
                            </div>

                            <asp:Button ID="ShowPopup" runat="server" Text="Button" class="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
                                <div class="modal-dialog modal-sm">
                                    <div class="modal-content">

                                        <div class="modal-header">
                                            <h4 class="modal-title" id="myModalLabel2"></h4>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                                <span aria-hidden="true">×</span>
                                            </button>
                                        </div>
                                        <div class="modal-body">
                                        </div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                                        </div>

                                    </div>
                                </div>
                            </div>
                        </div>

                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Manage : Data</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvPreview" runat="server" 
                                            EnableViewState="false" 
                                            CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                            AutoGenerateColumns="False" GridLines="None">
                                            <Columns>
                                                <asp:BoundField DataField="Zone" HeaderText="Zone" />
                                                <asp:BoundField DataField="User_Id" HeaderText="User Id" />
                                                <asp:BoundField DataField="User_Name" HeaderText="User Name" />
                                                <asp:BoundField DataField="Month" HeaderText="Month" />
                                                <asp:BoundField DataField="Day" HeaderText="Day" />
                                                <asp:BoundField DataField="Date" HeaderText="Date" DataFormatString="{0:dd-MM-yyyy}" />
                                                <asp:BoundField DataField="Attendance_Status" HeaderText="Attendance" />
                                                <asp:BoundField DataField="Total_Distance_Travelled_In_KM" HeaderText="Distance (KM)" />
                                            </Columns>
                                        </asp:GridView>

                                        <div class="col-md-12 col-sm-12" id="divErrorLog" runat="server" visible="false">
                                            <div class="x_panel" style="border: 1px solid #dc3545;">
                                                <div class="x_title">
                                                    <h2 class="text-danger"><i class="fa fa-exclamation-triangle"></i>Import Errors Found</h2>
                                                    <ul class="nav navbar-right panel_toolbox">
                                                        <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                                    </ul>
                                                    <div class="clearfix"></div>
                                                </div>
                                                <div class="x_content">
                                                    <p class="text-muted">The following rows could not be imported. Please fix these issues in your CSV and try uploading them again.</p>
                                                    <div class="card-box table-responsive">
                                                        <asp:GridView ID="gvErrors" runat="server" 
                                                            EnableViewState="false" 
                                                            CssClass="table table-striped table-bordered table-sm table-danger"
                                                            AutoGenerateColumns="False" GridLines="None">
                                                            <Columns>
                                                                <asp:BoundField DataField="StagingId" HeaderText="Row Ref ID" />
                                                                <asp:BoundField DataField="ErrorStage" HeaderText="Error Stage" />
                                                                <asp:BoundField DataField="ErrorReason" HeaderText="Reason for Failure" />

                                                                <%-- Optional: You can show the raw JSON or a specific column to help them identify the row --%>
                                                                <asp:BoundField DataField="RawJson" HeaderText="Raw Uploaded Data" />
                                                            </Columns>
                                                        </asp:GridView>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
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
