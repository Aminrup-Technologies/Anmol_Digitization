<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Pre_Dispatch_Detailed.aspx.cs" Inherits="AnmolDristi.Pre_Dispatch_Detailed" %>

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


        }

        window.onload = function () {
            // Disable  RadioButtonLists
            disableRadioButtonLists();

            // Toggle remarks divs based on the selected value for each RadioButtonList
            toggleAllRadioRemarksDivs();

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

        function toggleAllRadioRemarksDivs() {
            // Define a mapping of RadioButtonLists and their respective remarks divs
            var mapping = {
                '<%= RBL_BoxCondition.ClientID %>': 'BoxConditionRemarksDiv',
                '<%= RBL_Tapping.ClientID %>': 'TappingRemarksDiv',
                '<%= RBL_LongSeal.ClientID %>': 'LongSealRemarksDiv',
                '<%= RBL_EndSeal.ClientID %>': 'EndSealRemarksDiv',
                '<%= RBL_MainPanel.ClientID %>': 'MainPanelRemarksDiv',
                '<%= RBL_CutsPackets.ClientID %>': 'CutsPacketsRemarksDiv',
                '<%= RBL_BackingStatus.ClientID %>': 'BackingStatusRemarksDiv',
                '<%= RBL_ElongOval.ClientID %>': 'ElongOvalRemarksDiv',
                '<%= RBL_Cupping.ClientID %>': 'CuppingRemarksDiv',
                '<%= RBL_Impression.ClientID %>': 'ImpressionRemarksDiv',
                '<%= RBL_SoggyStatus.ClientID %>': 'SoggyStatusRemarksDiv',
                '<%= RBL_ForeignBody.ClientID %>': 'ForeignBodyRemarksDiv',
                '<%= RBL_OffOdour.ClientID %>': 'OffOdourRemarksDiv',
            };

            // Loop through each mapping and toggle the div visibility based on the selected value
            for (var rblId in mapping) {
                var remarksDivId = mapping[rblId];
                toggleRemarksDiv(rblId, remarksDivId);
            }
        }

        function toggleRemarksDiv(rblId, remarksDivId) {
            var rbl = document.getElementById(rblId);

            if (rbl) {
                // Get the selected value from the RadioButtonList
                var selectedValue = rbl.querySelector('input[type="radio"]:checked').value;

                var remarksDiv = document.getElementById(remarksDivId);

                // If "Not Ok" (Value = "0") is selected, show the remarks div, otherwise hide it
                if (selectedValue === "0") {
                    remarksDiv.style.display = "block";
                } else {
                    remarksDiv.style.display = "none";
                }
            }
        }

    </script>

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="QA - PRE DISPATCH CLEARANCE REPORT"></asp:Label>
                    </h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/CORP/QA/06"></asp:Label>
                            </h2>
                            <div class="clearfix"></div>
                        </div>

                        <%--form start--%>
                        <div class="row">
                            <div class="col-12">
                                <div class="card">
                                    <div class="card-body">
                                        <div class="row">
                                            <div class="col-12">
                                                <div class="x-content">

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>

                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>

                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
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
                                                            <asp:Label ID="lblInspectionLot" runat="server" AssociatedControlID="TXT_InspectionLot" Text="Inspection Lot :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_InspectionLot" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("InspectionLot") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblMaterialCode" runat="server" AssociatedControlID="TXT_MaterialCode" Text="Material Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_MaterialCode" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("MaterialCode") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblCBB_Produced" runat="server" AssociatedControlID="TXT_CBB_Produced" Text="No of CBB Produced :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_CBB_Produced" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CBB_Produced") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblCBB_Checked" runat="server" AssociatedControlID="TXT_CBB_Checked" Text="No of CBB Checked :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_CBB_Checked" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CBB_Checked") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelBoxCondition" runat="server" AssociatedControlID="RBL_BoxCondition" Text="Box Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_BoxCondition" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="BoxConditionRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelBoxConditionRemarks" runat="server" AssociatedControlID="TXB_BoxCondition_Remarks" Text="Box Condition (Not Ok)" ForeColor="IndianRed" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_BoxCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CBB_Box_ConditionRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblPackets_CBB" runat="server" AssociatedControlID="TXT_Packets_CBB" Text="No of Packets in CBB :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_Packets_CBB" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("Packets_CBB") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>


                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelTapping" runat="server" AssociatedControlID="RBL_Tapping" Text="Tapping of CBB :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_Tapping" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="TappingRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelTappingRemarks" runat="server" AssociatedControlID="TXB_Tapping_Remarks" Text="Tapping of CBB (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Tapping_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("CBB_tappingRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblPkts_Checked_Per_CBB" runat="server" AssociatedControlID="TXT_Pkts_Checked_Per_CBB" Text="PKTS Checked/ CBB Box:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_Pkts_Checked_Per_CBB" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("PacketsChecked_CBB") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblWt_of_Pkts" runat="server" AssociatedControlID="TXT_Wt_of_Pkts" Text="Wt. of Pkts (in gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_Wt_of_Pkts" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("WeightofPackets") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblPackageDate" runat="server" AssociatedControlID="TXT_PackageDate" Text="Pkg Date:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_PackageDate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("PackageDate","{0:dd-MM-yyyy}") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblBatchNo" runat="server" AssociatedControlID="TXT_BatchNo" Text="Batch/Lot No:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_BatchNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("BatchNo") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="lblPacketsMRP" runat="server" AssociatedControlID="TXT_PacketsMRP" Text="MRP of Pkts (in ₹):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXT_PacketsMRP" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("PacketsMRP") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelLongSeal" runat="server" AssociatedControlID="RBL_LongSeal" Text="Long Seal Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_LongSeal" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="LongSealRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelLongSealRemarks" runat="server" AssociatedControlID="TXB_LongSeal_Remarks" Text="Long Seal Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_LongSeal_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("LongSealRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelEndSeal" runat="server" AssociatedControlID="RBL_EndSeal" Text="End Seal Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_EndSeal" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="EndSealRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelEndSealRemarks" runat="server" AssociatedControlID="TXB_EndSeal_Remarks" Text="End Seal Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_EndSeal_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("EndSealRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelMainPanel" runat="server" AssociatedControlID="RBL_MainPanel" Text="Main Panel Central/ Not :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_MainPanel" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="MainPanelRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelMainPanelRemarks" runat="server" AssociatedControlID="TXB_MainPanel_Remarks" Text="Main Panel Central (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_MainPanel_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("MainPanelRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelCutsPackets" runat="server" AssociatedControlID="RBL_CutsPackets" Text="Cuts in Packets :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_CutsPackets" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="CutsPacketsRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelCutsPacketsRemarks" runat="server" AssociatedControlID="TXB_CutsPackets_Remarks" Text="Cuts in Packets (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_CutsPackets_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("Cuts_PacketsRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelBackingStatus" runat="server" AssociatedControlID="RBL_BackingStatus" Text="Baking (Under/Over Baked) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_BackingStatus" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Under/Over Baked" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="BackingStatusRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelBackingStatusRemarks" runat="server" AssociatedControlID="TXB_BackingStatus_Remarks" Text="Baking of Biscuits (Under/Over Baked)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_BackingStatus_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("BackingStatusRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelElongOval" runat="server" AssociatedControlID="RBL_ElongOval" Text="Shape Condition (Elongated or Oval) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_ElongOval" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="ElongOvalRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelElongOvalRemarks" runat="server" AssociatedControlID="TXB_ElongOval_Remarks" Text="Shape Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_ElongOval_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("ElongOvalRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelCupping" runat="server" AssociatedControlID="RBL_Cupping" Text="Cupping Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_Cupping" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="CuppingRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelCuppingRemarks" runat="server" AssociatedControlID="TXB_Cupping_Remarks" Text="Cupping Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Cupping_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Text='<%#  Eval("CuppingRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelImpression" runat="server" AssociatedControlID="RBL_Impression" Text="Impression on Biscuits :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_Impression" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="ImpressionRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelImpressionRemarks" runat="server" AssociatedControlID="TXB_Impression_Remarks" Text="Impression on Biscuits (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Impression_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("ImpressionRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelSoggyStatus" runat="server" AssociatedControlID="RBL_SoggyStatus" Text="Biscuit Soggy:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_SoggyStatus" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="SoggyStatusRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelSoggyStatusRemarks" runat="server" AssociatedControlID="TXB_SoggyStatus_Remarks" Text="Biscuit Soggy (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_SoggyStatus_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("SoggyStatusRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelForeignBody" runat="server" AssociatedControlID="RBL_ForeignBody" Text="Foreign Body in Biscuit :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_ForeignBody" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="ForeignBodyRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelForeignBodyRemarks" runat="server" AssociatedControlID="TXB_ForeignBody_Remarks" Text="Foreign Body in Biscuit (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_ForeignBody_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("ForeignBodyRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelOffOdour" runat="server" AssociatedControlID="RBL_OffOdour" Text="Off Odour (Yes/No) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_OffOdour" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                                                    <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="OffOdourRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelOffOdourRemarks" runat="server" AssociatedControlID="TXB_OffOdour_Remarks" Text="Off Odour (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_OffOdour_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%#  Eval("OffOdourRemarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelRemarks" runat="server" AssociatedControlID="TXB_Remarks" Text="Additional Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Remarks" runat="server" ReadOnly="true" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Remarks") %>' TextMode="MultiLine" Rows="5" Width="100%"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <%--Buttons--%>
                                                    <div class="col-md-12">
                                                        <div class="mb-12">
                                                            <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group input-group-sm">
                                                                <asp:Button ID="btnSubmit" runat="server" Text="Re-Validate Inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="Submit" CausesValidation="true" />
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
                                <%-- form end--%>
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
