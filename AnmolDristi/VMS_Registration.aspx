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
                    <asp:HiddenField ID="hfIsFacilityOpen" runat="server" />
                    <asp:HiddenField ID="hfPlantTimingsJson" runat="server" />
                    <!-- 2026-08-22: Added hfCameraActive to track whether getUserMedia succeeded. Used by client+server validation to distinguish camera capture from manual POST. -->
                    <asp:HiddenField ID="hfCameraActive" runat="server" Value="false" />

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

                    <!-- 2026-08-22: Company Name validation — RequiredFieldValidator added. Mandatory administrative data. -->
                    <div class="form-group">
                        <label class="form-label">Company Name <span style="color: red;">*</span></label>
                        <asp:TextBox ID="txtCompany" runat="server" CssClass="form-control" placeholder="Where are you from?"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvCompany" runat="server" ControlToValidate="txtCompany"
                            ErrorMessage="Company Name is required." Display="Dynamic"
                            ForeColor="Red" Font-Size="Small" SetFocusOnError="true"
                            ValidationGroup="Registration" CssClass="text-danger" />
                    </div>

                    <!-- 2026-08-22: Designation validation — label updated with *, RequiredFieldValidator added. Mandatory administrative data. -->
                    <div class="form-group">
                        <label class="form-label">Designation <span style="color: red;">*</span></label>
                        <asp:TextBox ID="txtDesignation" runat="server" CssClass="form-control" placeholder="Your Job Title"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvDesignation" runat="server" ControlToValidate="txtDesignation"
                            ErrorMessage="Designation is required." Display="Dynamic"
                            ForeColor="Red" Font-Size="Small" SetFocusOnError="true"
                            ValidationGroup="Registration" CssClass="text-danger" />
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

                    <!-- 2026-08-22: ValidationGroup="Registration" added to coordinate ASP.NET validators. -->
                    <asp:Button ID="btnSubmit" runat="server" CssClass="btn btn-proceed" Text="Submit Registration" OnClientClick="return validateForm();" OnClick="btnSubmit_Click" ValidationGroup="Registration" />
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

            // 2026-08-22: Camera initialization updated — hfCameraActive flag set on successful getUserMedia.
            // This flag is checked in validateForm() and sent to server for validation.
            if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" } })
                    .then(function (stream) {
                        video.srcObject = stream;
                        document.getElementById('<%= hfCameraActive.ClientID %>').value = "true";
                    })
                    .catch(function (error) {
                        console.error("Camera Error:", error);
                        document.getElementById('<%= hfCameraActive.ClientID %>').value = "false";
                        alert("Camera access is required to take a visitor photo. Please enable camera permissions and refresh the page.");
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
        // 2026-08-22: validateForm() updated — Company Name, Designation mandatory checks added.
        // Camera activation check added to require live camera capture.
        function validateForm() {
            // Company Name mandatory
            var companyVal = $("#<%= txtCompany.ClientID %>").val();
            if (!companyVal || companyVal.trim() === "") {
                alert("Please enter your Company Name.");
                $("#<%= txtCompany.ClientID %>").focus();
                return false;
            }

            // Designation mandatory
            var designationVal = $("#<%= txtDesignation.ClientID %>").val();
            if (!designationVal || designationVal.trim() === "") {
                alert("Please enter your Designation.");
                $("#<%= txtDesignation.ClientID %>").focus();
                return false;
            }

            // Photo mandatory — requires camera activation and capture
            if ($("#<%= hfCameraActive.ClientID %>").val() !== "true") {
                alert("Camera access is required. Please allow camera permissions and take a photo.");
                return false;
            }
            if ($("#<%= hfSelfieBase64.ClientID %>").val() === "") {
                alert("Please take a photo using the camera before submitting.");
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

            // Off-Hours Soft Warning — current time and/or scheduled window (one notice)
            var hoursNotice = buildVisitorHoursNotice();
            if (hoursNotice) {
                var proceed = confirm(hoursNotice);
                if (!proceed) {
                    return false;
                }
            }
            
            // 🌟 ANTI-SPAM FIX: Hide button instantly and show 'Processing...'
            var submitBtn = $("#<%= btnSubmit.ClientID %>");
            submitBtn.hide(); 
            submitBtn.after('<button type="button" class="btn btn-proceed" disabled><i class="fa fa-spinner fa-spin"></i> Processing...</button>');
            
            return true;
        }

        function parseTimeSpan(value) {
            if (!value) return null;
            var parts = value.split(":");
            if (parts.length < 2) return null;
            var h = parseInt(parts[0], 10);
            var m = parseInt(parts[1], 10);
            if (isNaN(h) || isNaN(m)) return null;
            return (h * 60) + m;
        }

        function weekdayName(dateValue) {
            var names = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
            var d = new Date(dateValue + "T00:00:00");
            return names[d.getDay()];
        }

        function isWindowOutsideTiming(startMin, endMin, timing) {
            if (!timing) return false;
            if (!timing.open && !timing.close) return true;
            var openMin = parseTimeSpan(timing.open);
            var closeMin = parseTimeSpan(timing.close);
            if (openMin !== null && startMin < openMin) return true;
            if (closeMin !== null && (startMin >= closeMin || endMin > closeMin)) return true;
            return false;
        }

        function formatNoticeDate(dateValue) {
            var d = new Date(dateValue + "T00:00:00");
            var months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
            return weekdayName(dateValue) + " " + ("0" + d.getDate()).slice(-2) + " " + months[d.getMonth()] + " – Outside hours";
        }

        function buildVisitorHoursNotice() {
            var timings = {};
            try {
                var raw = $("#<%= hfPlantTimingsJson.ClientID %>").val();
                if (raw) timings = JSON.parse(raw);
            } catch (e) {
                timings = {};
            }

            var now = new Date();
            var todayName = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"][now.getDay()];
            var nowMin = now.getHours() * 60 + now.getMinutes();
            var currentAfter = isWindowOutsideTiming(nowMin, nowMin, timings[todayName]);

            var fromDate = $("#<%= txtVisitFromDate.ClientID %>").val();
            var toDate = $("#<%= ddlScheduleType.ClientID %>").val() === "MultiDay"
                ? $("#<%= txtVisitToDate.ClientID %>").val()
                : fromDate;
            var timeFrom = parseTimeSpan($("#<%= txtTimeFrom.ClientID %>").val());
            var timeTo = parseTimeSpan($("#<%= txtTimeTo.ClientID %>").val());

            var affected = [];
            if (fromDate && timeFrom !== null && timeTo !== null) {
                var cursor = new Date(fromDate + "T00:00:00");
                var end = new Date((toDate || fromDate) + "T00:00:00");
                if (end < cursor) end = cursor;
                while (cursor <= end) {
                    var iso = cursor.getFullYear() + "-" +
                        ("0" + (cursor.getMonth() + 1)).slice(-2) + "-" +
                        ("0" + cursor.getDate()).slice(-2);
                    if (isWindowOutsideTiming(timeFrom, timeTo, timings[weekdayName(iso)])) {
                        affected.push(iso);
                    }
                    cursor.setDate(cursor.getDate() + 1);
                }
            }

            if (!currentAfter && affected.length === 0)
                return "";

            var msg = "Notice: Your visit falls outside the organization's normal working hours. Your host may still approve this request as an exception.";
            if (currentAfter && affected.length > 0)
                msg += "\n\nYou are submitting outside current plant hours, and the requested visit window is also outside hours.";
            else if (currentAfter)
                msg += "\n\nYou are submitting outside current plant hours.";
            else
                msg += "\n\nThe requested visit window is outside plant hours.";

            if (affected.length > 0) {
                msg += "\n";
                for (var i = 0; i < affected.length; i++) {
                    msg += "\n- " + formatNoticeDate(affected[i]);
                }
            }
            return msg;
        }
    </script>
</asp:Content>