<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Process_FinalApproval.aspx.cs" Inherits="AnmolDristi.Process_FinalApproval" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }

        .white-background-readonly {
            background-color: white !important;
            color: black !important;
            cursor: default;
        }

        .custom-page-title {
            width: 100%;
            background-color: #f0f0f0; /* Example: Change background color */
            margin-top: 20px;
            padding-top: 35px;
            padding-right: 20px;
            padding-left: 20px;
            padding-bottom: 20px;
        }

            .custom-page-title .title_left h3 {
                font-size: 20px;
                color: #333333;
                font-weight: bold;
            }

        /*.nav-tabs .nav-link.active {
            background-color: #17a2b8;
            color: white;
            border: 2px solid #17a2b8;
            border-radius: 5px;
        }

        .nav-tabs .nav-link:hover {
       
            color: #17a2b8;
        }*/

        .approver-photo {
            width: 50px;
            height: 50px;
            border-radius: 50%;
            object-fit: cover;
        }

        .approver-flow {
            display: flex;
            align-items: center;
            justify-content: space-around;
            padding: 1rem;
            background-color: #f8f9fa;
            border: 1px solid #ddd;
            border-radius: .25rem;
        }

        .flow-line {
            flex: 1;
            border-top: 2px solid #007bff;
            margin: 0 10px;
        }

        .approver-item {
            text-align: center;
        }
    </style>


</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script type="text/javascript">

        function validateOnButtonClick() {

            toggleAllTextboxVisibility();
            validateWgtGridView();
        }

        window.onload = function () {
            // Disable all RadioButtonLists
            disableRadioButtonLists();

            // Toggle remarks divs based on the selected value for each RadioButtonList
            toggleAllRemarksDivs();

            // Check and toggle textboxes based on the value range
            toggleAllTextboxVisibility();
        };

        function disableRadioButtonLists() {
            // Get all RadioButtonLists by their class
            var radioButtonLists = document.querySelectorAll('.form-control');

            radioButtonLists.forEach(function (rbl) {
                var radioItems = rbl.getElementsByTagName('input');
                for (var i = 0; i < radioItems.length; i++) {
                    radioItems[i].disabled = true; // Disable interaction
                }
            });
        }

        function toggleAllTextboxVisibility() {
            // Array of textboxes and their corresponding remarks divs
            var textboxMappings = [
                { textboxId: '<%= TB_RoomTemp.ClientID %>', remarksDivId: 'RoomTempRemarksDiv', errorMsgId: 'RoomTempErrorMsg', }, //Sponge
                { textboxId: '<%= TB_Temp.ClientID %>', remarksDivId: 'TempRemarksDIV', errorMsgId: 'TempErrorMsg', },       //Sponge
                { textboxId: '<%= TB_DoughTemp.ClientID %>', remarksDivId: 'DoughTempRemarksDIV', errorMsgId: 'DoughTempErrorMsg', }  //Dough
            ];

            // Loop through each mapping and check the value range
            textboxMappings.forEach(function (mapping) {
                var textbox = document.getElementById(mapping.textboxId);
                var remarksDiv = document.getElementById(mapping.remarksDivId);
                var errorMsg = document.getElementById(mapping.errorMsgId);

                var value = parseFloat(textbox.value); // Get the value of the textbox

                // Toggle visibility based on the value range (less than 20 or greater than 60)
                if (value < 20 || value > 60) {
                    remarksDiv.style.display = "block"; // Show remarks div
                    errorMsg.style.display = "inline"; // Show error message
                } else {
                    remarksDiv.style.display = "none"; // Hide remarks div
                    errorMsg.style.display = "none";   // Hide error message
                }
            });
        }

        function toggleAllRemarksDivs() {
            // Define a mapping of RadioButtonLists and textboxes to their respective remarks divs
            var mapping = {
                // Basic
                'RBL_MaidaColorApp': 'MaidaColorAppRemarksDiv',
                'RBL_MaidaFlavorTaste': 'MaidaFlavorTasteRemarksDiv',
                'RBL_MaidaGrittiness': 'MaidaGrittinessRemarksDiv',
                'RBL_BBColorApp': 'BBColorAppRemarksDiv',
                'RBl_BBFlavorTaste': 'BBFlavorTasteRemarksDiv',
                'RBL_BBMouthFeel': 'BBMouthFeelRemarksDiv',
                'RBL_HvoSmell': 'HvoSmellRemarksDiv',
                'RBL_HvoTaste': 'HvoTasteRemarksDiv',
                'RBL_SMPSmell': 'SMPSmellRemarksDiv',
                'RBL_SMPTaste': 'SMPTasteRemarksDiv',
                'RBL_SMPColor': 'SMPColorRemarksDiv',
                'RBL_SyrupColor': 'SyrupColorRemarksDiv',
                'RBL_InvertSyrupBucket': 'InvertSyrupBucketRemarksDiv',
                'RBL_SugarSolBucket': 'SugarSolBucketRemarksDiv',
                'RBL_CreamerBucketFilter': 'CreamerBucketFilterRemarksDiv',
                'RBL_SugarGrinder': 'SugarGrinderRemarksDiv',
                'RBL_OilSystem': 'OilSystemRemarksDiv',
                'RBL_OilSpray': 'OilSprayRemarksDiv',
                'RBL_MilkSpray': 'MilkSprayRemarksDiv',
                'RBL_DrumCovered': 'DrumCoveredRemarksDiv',
                'RBL_Quality': 'QualityRemarksDiv',
                'RBL_MetalDectector': 'MetalDetectorRemarksDiv',
                'RBL_ProcessSequence': 'ProcessSequenceRemarksDiv',
                'RBL_DoughCondition': 'DoughConditionRemarksDiv',
                'RBL_BalanceCondition': 'BalanceConditionRemarksDiv',
            };

            for (var rblId in mapping) {
                var remarksDivId = mapping[rblId];
                toggleRemarksDiv(rblId, remarksDivId);
            }
        }

        function toggleRemarksDiv(rblId, remarksDivId) {
            var rbl = document.getElementById(rblId);
            var selectedValue = rbl.querySelector('input[type="radio"]:checked')?.value;

            var remarksDiv = document.getElementById(remarksDivId);

            if (selectedValue === "0") { // If "Not Ok" is selected
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateWgtGridView() {
            var deviationTextBoxes = document.querySelectorAll('[id$="txtDeviationPercentage"]');

            // Loop through each textbox and check its value
            deviationTextBoxes.forEach(function (textBox) {
                var deviationPercentage = parseFloat(textBox.value);

                // Get the row of the current textbox
                var row = textBox.closest('tr');

                // Check if deviation is greater than 5%
                if (!isNaN(deviationPercentage) && Math.abs(deviationPercentage) > 5) {
                    row.style.borderColor = "red"; // Highlight row in red
                } else {
                    row.style.borderColor = ""; // Reset border color if within range
                }
            });
        }

    </script>

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="QA - Process Checking Report"></asp:Label></h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/CORP/QA/02"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="row">
                            <div class="col-12">
                                <div class="card">
                                    <div class="card-body">
                                        <div class="row">
                                            <div class="col-12">
                                                <ul class="nav nav-tabs mb-4" id="myTab" role="tablist">
                                                    <li class="nav-item">
                                                        <a class="nav-link active text" id="basicData-tab" data-toggle="tab" href="#basicData" role="tab" aria-controls="basicData" aria-selected="true">
                                                            <%--<i class="fa fa-id-badge mr-2"></i>--%>Plant & Line</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text" id="weight-tab" data-toggle="tab" href="#weight" role="tab" aria-controls="weight" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Raw Weight</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link " id="spongeData-tab" data-toggle="tab" href="#spongeData" role="tab" aria-controls="spongeData" aria-selected="false">Sponge </a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link " id="doughData-tab" data-toggle="tab" href="#doughData" role="tab" aria-controls="doughData" aria-selected="false">Dough </a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link " id="ovenData-tab" data-toggle="tab" href="#ovenData" role="tab" aria-controls="ovenData" aria-selected="false">Oven </a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link " id="verificationData-tab" data-toggle="tab" href="#verificationData" role="tab" aria-controls="verificationData" aria-selected="false">Verification</a>
                                                    </li>
                                                </ul>

                                                <div class="tab-content ml-1" id="myTabContent">

                                                    <%---Basic Data Starts--%>
                                                    <div class="tab-pane fade show active" id="basicData" role="tabpanel" aria-labelledby="basicData-tab">
                                                        <div class="x-content">

                                                            <div class="col-md-3" id="CompleteTab1" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Img_Success1" runat="server" ImageUrl="~/WebData/success_gif.gif" CssClass="pull-left align-content-center" Width="100px" Height="100px" />
                                                                    <asp:Label ID="lbl_tbtidcreatedmsg" runat="server" Text="Record ID Created..!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                    : [<asp:Label ID="lbl_recordid1" runat="server" Text="" Visible="false" Font-Bold="true" ForeColor="Black"></asp:Label>]
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="IncompleteTab1" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image4" runat="server" ImageUrl="~/WebData/crossgif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label6" runat="server" Text="Incomplete Submission!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-2">
                                                                <div class="mb-2">
                                                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-2">
                                                                <div class="mb-2">
                                                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-2">
                                                                <div class="mb-2">
                                                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-2">
                                                                <div class="mb-2">
                                                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_ProcessWaterTemp" runat="server" AssociatedControlID="TB_ProcessWaterTemp" Text="Process Water Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_ProcessWaterTemp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("ProcessWaterTemp") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_WaterPH" runat="server" AssociatedControlID="TB_WaterPH" Text="Water PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_WaterPH" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("WaterPH") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>



                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_WaterHardness" runat="server" AssociatedControlID="TB_WaterHardness" Text="Water Hardness" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_WaterHardness" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("WaterHardness")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_WaterTest" runat="server" AssociatedControlID="TB_WaterTest" Text="Water Test :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_WaterTest" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("WaterTest") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_TDS" runat="server" AssociatedControlID="TB_TDS" Text="TDS" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_TDS" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("TDS") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_MaidaBrand" runat="server" AssociatedControlID="TB_MaidaBrand" Text="Maida Brand :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MaidaBrand" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("MaidaBrand") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_MaidaBatchNo" runat="server" AssociatedControlID="TB_MaidaBatchNo" Text="Maida Batch No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MaidaBatchNo" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("MaidaBatchNo")%>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_MaidaMfg" runat="server" AssociatedControlID="TB_MaidaMfg" Text="Maida Mfg :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MaidaMfg" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("MaidaMfgDate","{0:dd-MM-yyyy}") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaColorApp" runat="server" AssociatedControlID="RBL_MaidaColorApp" Text="Maida Color Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MaidaColorApp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MaidaColorAppRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaColorAppRemarks" runat="server" AssociatedControlID="TXB_MaidaColorApp_Remarks" Text="Maida Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MaidaColorApp_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CommentForMaidaColor")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaFlavorTaste" runat="server" AssociatedControlID="RBL_MaidaFlavorTaste" Text="Maida Flavor Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MaidaFlavorTaste" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MaidaFlavorTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaFlavorTasteRemarks" runat="server" AssociatedControlID="TXB_MaidaFlavorTaste_Remarks" Text="Maida Flavor Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MaidaFlavorTaste_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentsForMaidaFlavourAndTaste") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaGrittiness" runat="server" AssociatedControlID="RBL_MaidaGrittiness" Text="Maida Grittiness :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MaidaGrittiness" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MaidaGrittinessRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaGrittinessRemarks" runat="server" AssociatedControlID="TXB_MaidaGrittiness_Remarks" Text="Maida Grittiness (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MaidaGrittiness_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForGrittiness") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBColorApp" runat="server" AssociatedControlID="RBL_BBColorApp" Text="Broken Biscuit Color Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BBColorApp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BBColorAppRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBColorAppRemarks" runat="server" AssociatedControlID="TXB_BBColorApp_Remarks" Text="BB Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BBColorApp_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CommentForBBColor") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBFlavorTaste" runat="server" AssociatedControlID="RBL_BBFlavorTaste" Text="Broken Biscuit Flavor Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BBFlavorTaste" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BBFlavorTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBFlavorTasteRemarks" runat="server" AssociatedControlID="TXB_BBFlavorTaste_Remarks" Text="BB Flavor Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BBFlavorTaste_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForBBMouthFeel") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBMouthFeel" runat="server" AssociatedControlID="RBL_BBMouthFeel" Text="Broken Biscuit Mouth Feel :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BBMouthFeel" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BBMouthFeelRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBMouthFeelRemarks" runat="server" AssociatedControlID="TXB_BBMouthFeel_Remarks" Text="BB Mouth Feel (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BBMouthFeel_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForBBFlavorAndTaste") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoSmell" runat="server" AssociatedControlID="RBL_HvoSmell" Text="HVO Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_HvoSmell" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="HvoSmellRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoSmellRemarks" runat="server" AssociatedControlID="TXB_HvoSmell_Remarks" Text="HVO Smell (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_HvoSmell_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForHvoSmell") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTaste" runat="server" AssociatedControlID="RBL_HvoTaste" Text="HVO Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_HvoTaste" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="HvoTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTasteRemarks" runat="server" AssociatedControlID="TXB_HvoTaste_Remarks" Text="HVO Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_HvoTaste_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForHvoTaste") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTemp" runat="server" AssociatedControlID="TB_HvoTemp" Text="HVO Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_HvoTemp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("HvoTemp") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPSmell" runat="server" AssociatedControlID="RBL_SMPSmell" Text="SMP Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SMPSmell" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SMPSmellRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPSmellRemarks" runat="server" AssociatedControlID="TXB_SMPSmell_Remarks" Text="SMP Smell (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SMPSmell_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForSmpSmell") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPTaste" runat="server" AssociatedControlID="RBL_SMPTaste" Text="SMP Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SMPTaste" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SMPTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPTasteRemarks" runat="server" AssociatedControlID="TXB_SMPTaste_Remarks" Text="SMP Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SMPTaste_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForSmpTaste") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPColor" runat="server" AssociatedControlID="RBL_SMPColor" Text="SMP Color :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SMPColor" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SMPColorRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPColor_Remarks" runat="server" AssociatedControlID="TXB_SMPColor_Remarks" Text="SMP Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SMPColor_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForSmpColor") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupTemp" runat="server" AssociatedControlID="TB_HvoTemp" Text="Syrup Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_SyrupTemp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("SyrupTemp") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupColor" runat="server" AssociatedControlID="RBL_SyrupColor" Text="Syrup Color :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SyrupColor" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SyrupColorRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupColorRemarksDiv" runat="server" AssociatedControlID="TXB_SyrupColor_Remarks" Text="Syrup Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SyrupColor_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("SyrupColor") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_SyrupPH" runat="server" AssociatedControlID="TB_SyrupPH" Text="Syrup PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_SyrupPH" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("SyrupPH") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelInvertSyrupBucket" runat="server" AssociatedControlID="RBL_InvertSyrupBucket" Text="Invert Syrup Bucket Filter Sieve :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_InvertSyrupBucket" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="InvertSyrupBucketRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelInvertSyrupBucketRemarks" runat="server" AssociatedControlID="TXB_InvertSyrupBucket_Remarks" Text="Invert Syrup Bucket Filter Sieve Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_InvertSyrupBucket_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForISBF") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarSolBucket" runat="server" AssociatedControlID="RBL_SugarSolBucket" Text="Sugar Sol Bucket Filter Sieve :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SugarSolBucket" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SugarSolBucketRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarSolBucketRemarks" runat="server" AssociatedControlID="TXB_SugarSolBucket_Remarks" Text="Sugar Sol Bucket Filter Sieve Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SugarSolBucket_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForSSBF") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelCreamerBucketFilter" runat="server" AssociatedControlID="RBL_CreamerBucketFilter" Text="Creamer Bucket Filter Sheet :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_CreamerBucketFilter" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="CreamerBucketRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label12" runat="server" AssociatedControlID="TXB_CreamerBucket_Remarks" Text="Creamer Bucket Filter Sheet Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_CreamerBucket_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForCBF") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarGrinder" runat="server" AssociatedControlID="RBL_SugarGrinder" Text="Sugar Grinder Sheet :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SugarGrinder" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SugarGrinderRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarGrinderRemarks" runat="server" AssociatedControlID="TXB_SugarGrinder_Remarks" Text="Sugar Grinder Sheet Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SugarGrinder_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForSGS") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSystem" runat="server" AssociatedControlID="RBL_OilSystem" Text="Oil System Bucket Filter :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_OilSystem" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="OilSystemRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSystemRemarks" runat="server" AssociatedControlID="TXB_OilSystem_Remarks" Text="Oil System Bucket Filter Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_OilSystem_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForOSBF") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSpray" runat="server" AssociatedControlID="RBL_OilSpray" Text="Oil Spray Seive :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_OilSpray" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="OilSprayRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSprayRemarks" runat="server" AssociatedControlID="TXB_OilSpray_Remarks" Text="Oil Spray Seive Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_OilSpray_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("CommentForOilSpray") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMilkSpray" runat="server" AssociatedControlID="RBL_MilkSpray" Text="Milk Spray Seive :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MilkSpray" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MilkSprayRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label13" runat="server" AssociatedControlID="TXB_MilkSpray_Remarks" Text="Milk Spray Seive Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MilkSpray_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CommentForMilkSpray") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelColdRoomTemp" runat="server" AssociatedControlID="TB_ColdRoomTemp" Text="Temp. Of Cold Rooom:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_ColdRoomTemp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("ColdRoomTemp")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDeepFreezeTemp" runat="server" AssociatedControlID="TB_ColdRoomTemp" Text="Temp. Of Deep Freeze:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DeepFreezeTemp" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# "Deep Freeze:"  + Eval("DeepFreezeTemp") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <%--Image part--%>

                                                            <%--<div class="col-md-3" id="FU_MaidaImage_img" runat="server">
                                                                <asp:Label ID="LblMaiadImg" runat="server" Text="Maida Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                <asp:Image ID="imgMaida" runat="server" ImageUrl='<%# Eval("MaidaImageUrl") != null ? ResolveUrl(Eval("MaidaImageUrl").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="Maida Image" Width="100px" Height="100px" />
                                                            </div>--%>
                                                            <div class="col-md-3" id="FU_MaidaImage_img" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label37" runat="server" AssociatedControlID="imgMaida" Text="Maida Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Image ID="imgMaida" runat="server" CssClass="img-fluid" Width="100%" ImageUrl='<%# Eval("MaidaImageUrl") != null ? ResolveUrl(Eval("MaidaImageUrl").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="Maida Image" />
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <%--<div class="col-md-6" id="FU_BBImage_Img" runat="server">
                                                                <asp:Label ID="LblBBImg" runat="server" Text="Broken Biscuit Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                <asp:Image ID="imgBB" runat="server" ImageUrl='<%# Eval("BBImageUrl") != null ? ResolveUrl(Eval("BBImageUrl").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="BB Image" Width="100px" Height="100px" />
                                                            </div>--%>

                                                            <div class="col-md-3" id="FU_BBImage_Img" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label38" runat="server" AssociatedControlID="imgBB" Text="Maida Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Image ID="imgBB" runat="server" CssClass="img-fluid" Width="100%" ImageUrl='<%# Eval("BBImageUrl") != null ? ResolveUrl(Eval("BBImageUrl").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="BB Image" />
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <%-- Basic Data ends here--%>

                                                    <%-- Weight Data Starts Here--%>
                                                    <div class="tab-pane fade" id="weight" role="tabpanel" aria-labelledby="weight-tab">
                                                        <div class="x-content">
                                                            <div class="row" id="Div1" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image5" runat="server" ImageUrl="~/WebData/success_gif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label14" runat="server" Text="ID Created..!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                    : [<asp:Label ID="Label23" runat="server" Text="" Visible="false" Font-Bold="true"></asp:Label>]
                                                                </div>
                                                            </div>

                                                            <div class="row" id="Div2" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image6" runat="server" ImageUrl="~/WebData/crossgif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label24" runat="server" Text="Incomplete Submission!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                </div>
                                                            </div>

                                                            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>

                                                                    <asp:TemplateField HeaderText="Sl" HeaderStyle-Width="5%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtSl" runat="server" ClientIDMode="Static" Enabled="false" CssClass="form-control form-control-sm rounded" Text="<%# Container.DataItemIndex + 1 %>"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Variety" HeaderStyle-Width="25%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtVariety" runat="server" ReadOnly="true" ClientIDMode="Static" CssClass="form-control form-control-sm rounded" Text='<%# Eval("Variety") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Standard Weight" HeaderStyle-Width="10%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtStandardWeight" runat="server" ReadOnly="true" ClientIDMode="Static" CssClass="form-control form-control-sm rounded" Text='<%# Eval("StandardWeight") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Actual Weight" HeaderStyle-Width="15%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtActualWeight" runat="server" ReadOnly="true" ClientIDMode="Static" CssClass="form-control form-control-sm rounded" Text='<%# Eval("ActualWeight") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Deviation Weight" HeaderStyle-Width="15%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDeviation" runat="server" ReadOnly="true" ClientIDMode="Static" CssClass="form-control form-control-sm rounded" Text='<%# Eval("DeviationWeight") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Deviation Percentage" HeaderStyle-Width="20%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDeviationPercentage" runat="server" ReadOnly="true" ClientIDMode="Static" CssClass="form-control form-control-sm rounded" Text='<%# Eval("DeviationPercentage") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                </Columns>
                                                            </asp:GridView>

                                                            
                                                        </div>
                                                    </div>
                                                    <%-- Weight ends Starts Here--%>

                                                    <%--Sponge Data Start Here--%>
                                                    <div class="tab-pane fade" id="spongeData" role="tabpanel" aria-labelledby="spongeData-tab">
                                                        <div class="x-content">

                                                            <div class="row" id="Div3" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image7" runat="server" ImageUrl="~/WebData/success_gif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label25" runat="server" Text="ID Created..!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                    : [<asp:Label ID="Label26" runat="server" Text="" Visible="false" Font-Bold="true"></asp:Label>]
                                                                </div>
                                                            </div>

                                                            <div class="row" id="Div4" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image8" runat="server" ImageUrl="~/WebData/crossgif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label27" runat="server" Text="Incomplete Submission!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelRoomTemp" runat="server" AssociatedControlID="TB_RoomTemp" Text="Room Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_RoomTemp" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("RoomTemp")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="RoomTempRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label15" runat="server" AssociatedControlID="TXB_RoomTemp_Remarks" Text="Room Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_RoomTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("RoomTempCmnt")  %>'></asp:TextBox>
                                                                        <span id="RoomTempErrorMsg" forecolor="Red" display="Dynamic">[20.00-60.00]</span>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDrumCovered" runat="server" AssociatedControlID="RBL_DrumCovered" Text="Sponge Drum Covered :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_DrumCovered" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DrumCoveredRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label16" runat="server" AssociatedControlID="TXB_DrumCovered_Remarks" Text="Sponge Drum Covered (No)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DrumCovered_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("DrumCmnt")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelQuality" runat="server" AssociatedControlID="RBL_Quality" Text="Quality :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_Quality" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="QualityRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label17" runat="server" AssociatedControlID="TXB_Quality_Remarks" Text="Quality (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_Quality_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("QualityCmnt")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelStandingTime" runat="server" AssociatedControlID="TB_StandingTime" Text="Standing Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_StandingTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("StandingTime")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelTemp" runat="server" AssociatedControlID="TB_Temp" Text="Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Temp" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Temp")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="TempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label18" runat="server" AssociatedControlID="TXB_Temp_Remarks" Text="Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_Temp_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("TempCmnt")  %>'></asp:TextBox>
                                                                        <span id="TempErrorMsg" forecolor="Red" display="Dynamic">[20.00-60.00]</span>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            

                                                        </div>
                                                    </div>
                                                    <%-- Sponge Data Ends Here--%>

                                                    <%--Dough Data Start Here--%>
                                                    <div class="tab-pane fade" id="doughData" role="tabpanel" aria-labelledby="doughData-tab">
                                                        <div class="x-content">

                                                            <div class="row" id="Div5" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image9" runat="server" ImageUrl="~/WebData/success_gif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label28" runat="server" Text="ID Created..!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                    : [<asp:Label ID="Label29" runat="server" Text="" Visible="false" Font-Bold="true"></asp:Label>]
                                                                </div>
                                                            </div>

                                                            <div class="row" id="Div6" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image10" runat="server" ImageUrl="~/WebData/crossgif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label30" runat="server" Text="Incomplete Submission!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDoughTemp" runat="server" AssociatedControlID="TB_DoughTemp" Text="Dough Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DoughTemp" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("DoughTemp")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DoughTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label19" runat="server" AssociatedControlID="TXB_DoughTemp_Remarks" Text="Dough Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DoughTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("DoughTempCmnt")  %>'></asp:TextBox>
                                                                        <span id="DoughTempErrorMsg" forecolor="Red" display="Dynamic">[20.00-60.00]</span>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDoughRestTime" runat="server" AssociatedControlID="TB_DoughRestTime" Text="Dough Rest Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DoughRestTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("DoughRestTime")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMetalDectector" runat="server" AssociatedControlID="RBL_MetalDectector" Text="Metal Detector Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MetalDectector" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MetalDetectorRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label20" runat="server" AssociatedControlID="TXB_MetalDetector_Remarks" Text="Condition (No)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MetalDetector_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("DetectorCmnt")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelProcessSequence" runat="server" AssociatedControlID="RBL_ProcessSequence" Text="Process Sequence :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_ProcessSequence" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" ReadOnly="true" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="ProcessSequenceRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label21" runat="server" AssociatedControlID="TXB_ProcessSequence_Remarks" Text="Process Sequence (NoT Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_ProcessSequence_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("ProcessCmnt")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelCreamingTime" runat="server" AssociatedControlID="TB_CreamingTime" Text="Creaming Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_CreamingTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("CreamingTime")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMixingTime" runat="server" AssociatedControlID="TB_MixingTime" Text="Mixing Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MixingTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("MixingTime")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBakingTime" runat="server" AssociatedControlID="TB_BakingTime" Text="Baking Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BakingTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("BakingTime")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDiceRpm" runat="server" AssociatedControlID="TB_DoughTemp" Text="Dice RPM :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DiceRpm" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("DiceRpm")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDoughCondition" runat="server" AssociatedControlID="RBL_DoughCondition" Text="Condition Of Dough :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_DoughCondition" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" ReadOnly="true" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DoughConditionRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label22" runat="server" AssociatedControlID="TXB_DoughCondition_Remarks" Text="Dough Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughConditionRemarks" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="TXB_DoughCondition_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DoughCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("DoughCmnt")  %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            
                                                        </div>
                                                    </div>
                                                    <%--Dough Data Ends Here--%>

                                                    <%--Oven Data Start Here--%>
                                                    <div class="tab-pane fade" id="ovenData" role="tabpanel" aria-labelledby="ovenData-tab">
                                                        <div class="x-content">
                                                            <div class="row" id="Div7" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image11" runat="server" ImageUrl="~/WebData/success_gif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label31" runat="server" Text="ID Created..!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                    : [<asp:Label ID="Label32" runat="server" Text="" Visible="false" Font-Bold="true"></asp:Label>]
                                                                </div>
                                                            </div>

                                                            <div class="row" id="Div8" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image12" runat="server" ImageUrl="~/WebData/crossgif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label33" runat="server" Text="Incomplete Submission!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                </div>
                                                            </div>

                                                            <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" Width="100%" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>

                                                                    <asp:TemplateField HeaderText="Sl">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtSl" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded" ClientIDMode="Static" Text="<%# Container.DataItemIndex + 1 %>"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Zone">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtZone" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded" ClientIDMode="Static" Text='<%# Eval("Zone") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>


                                                                    <asp:TemplateField HeaderText="Oven Top">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtOvenTop" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded" ClientIDMode="Static" Text='<%# Eval("OvenTop") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Oven Bottom">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtOvenBottom" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded" ClientIDMode="Static" Text='<%# Eval("OvenBottom") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Damper Top">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDamperTop" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded" ClientIDMode="Static" Text='<%# Eval("DamperTop") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Damper Bottom">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDamperBottom" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded" ClientIDMode="Static" Text='<%# Eval("DamperBottom") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                </Columns>
                                                            </asp:GridView>


                                                            
                                                        </div>
                                                    </div>
                                                    <%--Oven Data Ends Here--%>

                                                    <%--Verification Data Start Here--%>
                                                    <div class="tab-pane fade" id="verificationData" role="tabpanel" aria-labelledby="verificationData-tab">
                                                        <div class="x-content">

                                                            <div class="row" id="Div9" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image13" runat="server" ImageUrl="~/WebData/success_gif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label34" runat="server" Text="ID Created..!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                    : [<asp:Label ID="Label35" runat="server" Text="" Visible="false" Font-Bold="true"></asp:Label>]
                                                                </div>
                                                            </div>

                                                            <div class="row" id="Div10" runat="server" visible="false">
                                                                <div class="col-md-12 col-sm-12" style="vertical-align: middle; text-align: center;">
                                                                    <asp:Image ID="Image14" runat="server" ImageUrl="~/WebData/crossgif.gif" Width="100px" Height="100px" />
                                                                    <asp:Label ID="Label36" runat="server" Text="Incomplete Submission!" Font-Bold="true" Font-Size="Large"></asp:Label>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label10" runat="server" AssociatedControlID="RBL_BalanceCondition" Text="Weighing Balance Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BalanceCondition" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BalanceConditionRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label46" runat="server" AssociatedControlID="TXB_BalanceCondition_Remarks" Text="Weighing Balance Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BalanceCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%# Eval("WghtBalanceCmnt") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelRawBiscuitWgt" runat="server" AssociatedControlID="TB_RawBiscuitWgt" Text="Raw Biscuit Weight:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_RawBiscuitWgt" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%# Eval("RawBiscuitWgt") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            

                                                        </div>
                                                    </div>

                                                    <%--<div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_BasicbtnApprove" runat="server" AssociatedControlID="" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group input-group-sm">
                                                                <asp:Button ID="BtnValidate" runat="server" Text="Re-Validate inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="" CausesValidation="true" OnClientClick="validateOnClick(); return false;" />
                                                                <asp:Button ID="BtnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="BtnApprove_Click" />
                                                                <asp:Button ID="BtnReject" runat="server" Text="Reject" CssClass="btn btn-sm btn-danger" CausesValidation="false" OnClick="BtnReject_Click" />
                                                            </div>
                                                        </div>
                                                    </div>--%>

                                                    <div class="col-md-12">
                                                        <div class="mb-12">
                                                            <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group input-group-sm">
                                                                <asp:Button ID="btnSubmit" runat="server" Text="Re-Validate Inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClientClick="validateOnClick(); return false;" />
                                                                <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="btnApprove_Click" />
                                                                <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btnReject_Click" />
                                                                <asp:Button ID="btnBack" runat="server" Text="Back" CssClass="btn btn-primary btn-sm" CausesValidation="false" OnClick="btnBack_Click" />
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

                    <div class="row">
                        <div class="col-md-12">
                            <div class="x_panel">
                                <div class="x_title">
                                    <h2>
                                        <asp:Label ID="Label8" runat="server" Text="Approval Matrix"></asp:Label></h2>
                                    <div class="clearfix"></div>

                                </div>
                                <div class="x_content">
                                    <!-- Approver Flow Diagram -->
                                    <div class="approver-flow">
                                        <div class="approver-item">
                                            <p>
                                                <asp:Label ID="Label9" runat="server" Text="Approver 1" />
                                            </p>
                                            <asp:Image ID="Image3" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                            <p>
                                                <asp:Label ID="Approver1NameLabel" runat="server" Text='<%# Eval("Approver1Name") %>' />
                                            </p>
                                            <p>
                                                <asp:Label ID="Approver1CodeLabel" runat="server" Text='<%# Eval("Approver1EmployeeCode") %>' />
                                            </p>
                                        </div>
                                        <div class="flow-line"></div>
                                        <div class="approver-item">
                                            <p>
                                                <asp:Label ID="Label7" runat="server" Text="Approver 2" />
                                            </p>
                                            <asp:Image ID="Image2" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                            <p>
                                                <asp:Label ID="Approver2NameLabel" runat="server" Text='<%# Eval("Approver2Name") %>' />
                                            </p>
                                            <p>
                                                <asp:Label ID="Approver2CodeLabel" runat="server" Text='<%# Eval("Approver2EmployeeCode") %>' />
                                            </p>
                                        </div>
                                        <div class="flow-line"></div>
                                        <div class="approver-item">
                                            <p>
                                                <asp:Label ID="Label11" runat="server" Text="Approver 3" />
                                            </p>
                                            <asp:Image ID="Image1" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                            <p>
                                                <asp:Label ID="DottedLineApproverNameLabel" runat="server" Text='<%# Eval("DottedLineApproverName") %>' />
                                            </p>
                                            <p>
                                                <asp:Label ID="DottedLineApproverCodeLabel" runat="server" Text='<%# Eval("DottedLineApproverEmployeeCode") %>' />
                                            </p>
                                        </div>
                                    </div>

                                    <hr />

                                    <!-- GridView for Detailed Information -->
                                    <asp:GridView ID="GridViewApprovers" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" Visible="false">
                                        <Columns>
                                            <asp:BoundField DataField="Approver1Name" HeaderText="Approver 1 Name" HtmlEncode="false" />
                                            <asp:BoundField DataField="Approver1EmployeeCode" HeaderText="Approver 1" HtmlEncode="false" />
                                            <asp:BoundField DataField="Approver2Name" HeaderText="Approver 2 Name" HtmlEncode="false" />
                                            <asp:BoundField DataField="Approver2EmployeeCode" HeaderText="Approver 2" HtmlEncode="false" />
                                            <asp:BoundField DataField="DottedLineApproverName" HeaderText="Dotted Line Approver Name" HtmlEncode="false" />
                                            <asp:BoundField DataField="DottedLineApproverEmployeeCode" HeaderText="Dotted Line Approver Code" HtmlEncode="false" />
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

</asp:Content>
