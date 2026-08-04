<%@ Page Title="Visitor Registration" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_Registration.aspx.cs" Inherits="AnmolDristi.VMS_Registration" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .mobile-form { background: #fff; padding: 25px; border-radius: 10px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); margin-top: 30px; }
        .form-label { font-weight: 600; color: #333; margin-top: 10px; }
        .btn-proceed { background-color: #1ABB9C; color: white; width: 100%; border-radius: 25px; padding: 12px; font-size: 16px; font-weight: bold; margin-top: 20px; }
        .camera-box { border: 2px dashed #1ABB9C; padding: 10px; border-radius: 8px; text-align: center; margin-bottom: 15px; }
        video, canvas { width: 100%; max-width: 300px; border-radius: 8px; }
        .ui-autocomplete { max-height: 200px; overflow-y: auto; overflow-x: hidden; z-index: 9999; }
        .quick-hosts-container .badge { display: inline-block; margin-bottom: 6px; font-size: 12px; font-weight: 600; }
        .quick-hosts-container .badge:hover { opacity: 0.85; }
    </style>
    <link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/base/jquery-ui.css">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main" style="background-color: #f7f7f7; min-height: 100vh;">
        <div class="row">
            <div class="col-md-5 col-md-offset-3 col-sm-8 col-sm-offset-2 col-xs-12">

                <asp:Panel ID="pnlRegistrationForm" runat="server" CssClass="mobile-form">
                    <div class="text-center">
                        <asp:Image ID="imgLogo" runat="server" ImageUrl="~/WebData/anmol_logo_GPTW.jpg" style="max-height: 80px; margin-bottom: 15px;" AlternateText="Anmol Logo" />
                    </div>
                    
                    <h3 class="text-center" style="color: #1ABB9C; font-weight: bold;">Complete Registration</h3>
                    <p class="text-center text-muted">Please provide your details and a photo.</p>
                    <div class="ln_solid"></div>

                    <asp:HiddenField ID="hfBranchId" runat="server" />
                    <asp:HiddenField ID="hfSelfieBase64" runat="server" />
                    <asp:HiddenField ID="hfSelectedHostCode" runat="server" />

                    <div class="form-group">
                        <label class="form-label">Visit Schedule *</label>
                        <asp:DropDownList ID="ddlScheduleType" runat="server" CssClass="form-control">
                            <asp:ListItem Text="Single Day" Value="SingleDay" Selected="True"></asp:ListItem>
                            <asp:ListItem Text="Multiple Days" Value="MultiDay"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="row">
                        <div class="col-xs-6">
                            <label class="form-label">Visit From Date *</label>
                            <asp:TextBox ID="txtVisitFromDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                        </div>
                        <div class="col-xs-6" id="divVisitToDate">
                            <label class="form-label">Visit To Date *</label>
                            <asp:TextBox ID="txtVisitToDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-xs-6">
                            <label class="form-label">Expected Arrival *</label>
                            <asp:TextBox ID="txtTimeFrom" runat="server" CssClass="form-control" TextMode="Time"></asp:TextBox>
                        </div>
                        <div class="col-xs-6">
                            <label class="form-label">Expected Departure *</label>
                            <asp:TextBox ID="txtTimeTo" runat="server" CssClass="form-control" TextMode="Time"></asp:TextBox>
                        </div>
                    </div>
                    <p class="text-muted" style="font-size: 12px; margin-bottom: 15px;">You may register anytime. Your host will approve the dates and daily hours. Security will check you in and out on each approved visit day.</p>

                    <div class="form-group">
                        <label class="form-label">Visitor Photo *</label>
                        <div class="camera-box">
                            <video id="videoElement" autoplay playsinline></video>
                            <canvas id="canvasElement" style="display: none;"></canvas>
                            <br />
                            <button type="button" id="btnSnap" class="btn btn-info btn-sm mt-2"><i class="fa fa-camera"></i>Take Photo</button>
                            <button type="button" id="btnRetake" class="btn btn-warning btn-sm mt-2" style="display: none;"><i class="fa fa-refresh"></i>Retake</button>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Purpose of Visit *</label>
                        <asp:DropDownList ID="ddlPurpose" runat="server" CssClass="form-control">
                            <asp:ListItem Text="-- Select Purpose --" Value=""></asp:ListItem>
                            <asp:ListItem Text="Internal Employee (I Work Here)" Value="I Work Here"></asp:ListItem>
                            <asp:ListItem Text="Meeting / Appointment" Value="Meeting Appointment"></asp:ListItem>
                            <asp:ListItem Text="Delivery" Value="Delivery"></asp:ListItem>
                            <asp:ListItem Text="Service / Maintenance" Value="Service"></asp:ListItem>
                            <asp:ListItem Text="Interview" Value="Interview"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div id="divInternalEmp" style="display: none;" class="form-group">
                        <label class="form-label">Your Employee Code *</label>
                        <asp:TextBox ID="txtInternalEmpCode" runat="server" CssClass="form-control" placeholder="Enter your Emp Code (e.g., AIL0000)"></asp:TextBox>
                        <small class="text-muted">Required for auto-approval bypass.</small>
                    </div>

                    <div class="row">
                        <div class="col-xs-6">
                            <label class="form-label">Mobile Number *</label>
                            <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control" ReadOnly="true"></asp:TextBox>
                        </div>
                        <div class="col-xs-6">
                            <label class="form-label">Email Address</label>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" TextMode="Email" placeholder="Optional — for gate pass link"></asp:TextBox>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-xs-6">
                            <label class="form-label">First Name *</label>
                            <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control" placeholder="First Name"></asp:TextBox>
                        </div>
                        <div class="col-xs-6">
                            <label class="form-label">Last Name *</label>
                            <asp:TextBox ID="txtLastName" runat="server" CssClass="form-control" placeholder="Last Name"></asp:TextBox>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Company Name *</label>
                        <asp:TextBox ID="txtCompany" runat="server" CssClass="form-control" placeholder="Where are you from?"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Designation</label>
                        <asp:TextBox ID="txtDesignation" runat="server" CssClass="form-control" placeholder="Your Job Title"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Whom to Meet *</label>
                        <input type="text" id="txtHostSearch" class="form-control" placeholder="Search employee name..." />
                        <small id="hostHelpText" class="text-muted">Select an active employee from the dropdown.</small>
                        <div id="divQuickHosts" runat="server" class="quick-hosts-container" style="margin-top: 10px;" visible="false">
                            <small class="text-muted">Or select a frequent host:</small><br />
                            <asp:Repeater ID="rptQuickHosts" runat="server">
                                <ItemTemplate>
                                    <span class="badge badge-info" style="cursor:pointer; margin-right:5px; padding:8px;"
                                        onclick="selectQuickHost('<%# Eval("EmpCode") %>', '<%# System.Web.HttpUtility.JavaScriptStringEncode(Convert.ToString(Eval("DisplayText"))) %>');">
                                        <%# Eval("DisplayText") %>
                                    </span>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <asp:Button ID="btnSubmit" runat="server" CssClass="btn btn-proceed" Text="Submit Registration" OnClientClick="return validateForm();" OnClick="btnSubmit_Click" />
                </asp:Panel>

                <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="mobile-form text-center">
                    <i class="fa fa-check-circle" style="font-size: 60px; color: #1ABB9C;"></i>
                    <h2>Pass Generated!</h2>
                    <p class="text-muted" id="pnlSuccessText" runat="server">Your pass has been generated. Please view it in 'My Visits'.</p>
                </asp:Panel>

                <div class="text-center" style="margin-top: 15px;">
                    <asp:Label ID="lblMessage" runat="server" Font-Bold="true"></asp:Label>
                </div>

            </div>
        </div>
    </div>

    <script type="text/javascript">
        $(document).ready(function () {
            function toggleScheduleFields() {
                if ($("#<%= ddlScheduleType.ClientID %>").val() === "MultiDay") {
                    $("#divVisitToDate").show();
                } else {
                    $("#divVisitToDate").hide();
                    $("#<%= txtVisitToDate.ClientID %>").val($("#<%= txtVisitFromDate.ClientID %>").val());
                }
            }

            $("#<%= ddlScheduleType.ClientID %>").change(toggleScheduleFields);
            $("#<%= txtVisitFromDate.ClientID %>").change(function () {
                if ($("#<%= ddlScheduleType.ClientID %>").val() !== "MultiDay") {
                    $("#<%= txtVisitToDate.ClientID %>").val($(this).val());
                }
            });
            toggleScheduleFields();

            // --- 0. UI Interaction: Internal Employee Logic ---
            $("#<%= ddlPurpose.ClientID %>").change(function () {
                if ($(this).val() === "I Work Here") {
                    $("#divInternalEmp").slideDown();
                    // Optional UX: Auto-fill company name
                    if ($("#<%= txtCompany.ClientID %>").val() === "") {
                        $("#<%= txtCompany.ClientID %>").val("Anmol Industries Ltd.");
                    }
                } else {
                    $("#divInternalEmp").slideUp();
                    $("#<%= txtInternalEmpCode.ClientID %>").val("");
                }
            });

            // --- 1. HTML5 Camera Logic ---
            const video = document.getElementById('videoElement');
            const canvas = document.getElementById('canvasElement');
            const snap = document.getElementById('btnSnap');
            const retake = document.getElementById('btnRetake');
            const hfSelfie = document.getElementById('<%= hfSelfieBase64.ClientID %>');

            if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" } })
                    .then(function (stream) {
                        video.srcObject = stream;
                    })
                    .catch(function (error) {
                        console.error("Camera Error:", error);
                        alert("Camera access is required to take a visitor photo.");
                    });
            }

            snap.addEventListener("click", function () {
                const context = canvas.getContext('2d');
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                context.drawImage(video, 0, 0, canvas.width, canvas.height);

                const dataUrl = canvas.toDataURL('image/png');
                hfSelfie.value = dataUrl;

                video.style.display = 'none';
                canvas.style.display = 'inline-block';
                snap.style.display = 'none';
                retake.style.display = 'inline-block';
            });

            retake.addEventListener("click", function () {
                hfSelfie.value = "";
                video.style.display = 'inline-block';
                canvas.style.display = 'none';
                snap.style.display = 'inline-block';
                retake.style.display = 'none';
            });

            // --- 2. Safe Host Search Autocomplete Logic ---
            function initializeAutocomplete() {
                var branchId = $("#<%= hfBranchId.ClientID %>").val();

                $("#txtHostSearch").autocomplete({
                    source: function (request, response) {
                        $.ajax({
                            url: "VMS_Registration.aspx/SearchHost",
                            data: JSON.stringify({ prefixText: request.term, branchId: parseInt(branchId) }),
                            dataType: "json",
                            type: "POST",
                            contentType: "application/json; charset=utf-8",
                            success: function (data) {
                                response($.map(data.d, function (item) {
                                    return { label: item.DisplayText, val: item.EmpCode }
                                }));
                            },
                            error: function (err) {
                                console.error("Autocomplete Error:", err);
                            }
                        });
                    },
                    select: function (e, i) {
                        $("#<%= hfSelectedHostCode.ClientID %>").val(i.item.val);
                        $("#txtHostSearch").val(i.item.label);
                        $("#txtHostSearch").css("border-color", "#1ABB9C");
                        $("#hostHelpText").text("Host Selected.").css("color", "#1ABB9C");
                        return false;
                    },
                    minLength: 2
                });
            }

            if (typeof $.ui === 'undefined' || typeof $.ui.autocomplete === 'undefined') {
                $.getScript("https://code.jquery.com/ui/1.12.1/jquery-ui.min.js")
                    .done(function () { initializeAutocomplete(); })
                    .fail(function () { console.error("Failed to load jQuery UI."); });
            } else {
                initializeAutocomplete();
            }
        });

        function selectQuickHost(empCode, displayText) {
            $("#<%= hfSelectedHostCode.ClientID %>").val(empCode);
            $("#txtHostSearch").val(displayText);
            $("#txtHostSearch").css("border-color", "#1ABB9C");
            $("#hostHelpText").text("Host Selected.").css("color", "#1ABB9C");
        }

        // Basic Client-Side Validation
        function validateForm() {
            if ($("#<%= hfSelfieBase64.ClientID %>").val() === "") {
                alert("Please take a selfie photo before submitting.");
                return false;
            }
            if ($("#<%= ddlPurpose.ClientID %>").val() === "") {
                alert("Please select the purpose of your visit.");
                return false;
            }
            if ($("#<%= ddlPurpose.ClientID %>").val() === "I Work Here" && $("#<%= txtInternalEmpCode.ClientID %>").val().trim() === "") {
                alert("Please enter your Employee Code to self-approve your pass.");
                return false;
            }
            if ($("#<%= hfSelectedHostCode.ClientID %>").val() === "") {
                alert("Please search and select a valid Host Employee.");
                return false;
            }
            if ($("#<%= txtVisitFromDate.ClientID %>").val() === "") {
                alert("Please select your visit date.");
                return false;
            }
            if ($("#<%= ddlScheduleType.ClientID %>").val() === "MultiDay" && $("#<%= txtVisitToDate.ClientID %>").val() === "") {
                alert("Please select your visit end date.");
                return false;
            }
            if ($("#<%= txtTimeFrom.ClientID %>").val() === "" || $("#<%= txtTimeTo.ClientID %>").val() === "") {
                alert("Please select your expected arrival and departure times.");
                return false;
            }
            
            // 🌟 ANTI-SPAM FIX: Hide button instantly and show 'Processing...'
            var submitBtn = $("#<%= btnSubmit.ClientID %>");
            submitBtn.hide(); 
            submitBtn.after('<button type="button" class="btn btn-proceed" disabled><i class="fa fa-spinner fa-spin"></i> Processing...</button>');
            
            return true;
        }
    </script>
</asp:Content>