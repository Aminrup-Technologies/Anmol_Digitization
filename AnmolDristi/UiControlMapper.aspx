<%@ Page Title="Ui Control Mapper" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="UiControlMapper.aspx.cs" Inherits="AnmolDristi.UiControlMapper" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Custom Stepper CSS matching Gentelella Theme */
        .wizard-steps {
            display: flex;
            padding: 0;
            margin: 10px 0 30px 0;
            list-style: none;
            position: relative;
        }

            .wizard-steps li {
                flex: 1;
                text-align: center;
                position: relative;
            }

                .wizard-steps li:before {
                    content: '';
                    position: absolute;
                    top: 18px;
                    left: 50%;
                    width: 100%;
                    height: 3px;
                    background: #e0e0e0;
                    z-index: 0;
                }

                .wizard-steps li:last-child:before {
                    display: none;
                }

                .wizard-steps li .step-num {
                    display: inline-block;
                    width: 40px;
                    height: 40px;
                    line-height: 40px;
                    border-radius: 50%;
                    background: #e0e0e0;
                    color: #fff;
                    font-weight: bold;
                    font-size: 16px;
                    position: relative;
                    z-index: 1;
                    border: 3px solid #fff;
                    box-shadow: 0 0 0 2px #e0e0e0;
                    transition: all 0.3s;
                }

                .wizard-steps li.active .step-num {
                    background: #1ABB9C;
                    box-shadow: 0 0 0 2px #1ABB9C;
                }

                .wizard-steps li.completed .step-num {
                    background: #26B99A;
                    box-shadow: 0 0 0 2px #26B99A;
                }

                .wizard-steps li.completed:before {
                    background: #26B99A;
                }

                .wizard-steps li .step-text {
                    display: block;
                    margin-top: 12px;
                    font-weight: 600;
                    color: #73879C;
                    font-size: 14px;
                }

        /* Step Panels */
        .step-panel {
            display: none;
            animation: fadeIn 0.4s;
            padding: 20px;
            border: 1px solid #e5e5e5;
            border-radius: 5px;
            background: #fafafa;
        }

            .step-panel.active {
                display: block;
            }

        @keyframes fadeIn {
            from {
                opacity: 0;
                transform: translateY(10px);
            }

            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        .mapper-box {
            height: 350px;
            overflow-y: auto;
            font-size: 14px;
            background: #fff;
        }

        .wizard-actions {
            margin-top: 20px;
            padding-top: 15px;
            border-top: 1px solid #e5e5e5;
            display: flex;
            justify-content: space-between;
        }

        /* Style for TreeView Checkboxes to look native to Gentelella */
        input[type="checkbox"] {
            margin-right: 8px;
            cursor: pointer;
        }

        .mapper-box {
            height: 400px;
            overflow-y: auto;
            background: #fff;
            padding: 10px;
            border-radius: 5px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-10 col-sm-12 offset-md-1">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>UI Control &rarr; Permission Mapper</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <asp:HiddenField ID="hfStep" runat="server" Value="1" />

                            <ul class="wizard-steps">
                                <li id="step-head-1">
                                    <span class="step-num">1</span>
                                    <span class="step-text">Select Page</span>
                                </li>
                                <li id="step-head-2">
                                    <span class="step-num">2</span>
                                    <span class="step-text">Select Control(s)</span>
                                </li>
                                <li id="step-head-3">
                                    <span class="step-num">3</span>
                                    <span class="step-text">Map & Save</span>
                                </li>
                            </ul>

                            <div id="step-panel-1" class="step-panel">
                                <h3 class="font-weight-bold mb-3 text-primary"><i class="fa fa-file-code-o"></i>Step 1: Target Page</h3>
                                <p class="text-muted">Select the ASP.NET page you want to secure. Selecting a page will automatically advance you to the next step.</p>

                                <div class="form-group">
                                    <div class="input-group">
                                        <asp:DropDownList ID="ddlPages" AutoPostBack="true" OnSelectedIndexChanged="ddlPages_ServerChange" runat="server" CssClass="form-control" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="btnScan" runat="server" Text="Rescan Project Files" OnClick="btnScan_Click" CssClass="btn btn-secondary" />
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <div id="step-panel-2" class="step-panel">
                                <h3 class="font-weight-bold mb-3 text-primary"><i class="fa fa-sitemap"></i>Step 2: Select Controls</h3>
                                <p class="text-muted">Check the boxes next to the panels or buttons you want to secure. You can select multiple items at once.</p>

                                <div class="form-group mt-3">
                                    <div class="mapper-box border p-2">
                                        <div class="mb-2">
                                            <div class="mb-2">
                                                <asp:Button ID="btnExpandAll" runat="server" Text="Expand All" CssClass="btn btn-xs btn-default" OnClick="btnExpandAll_Click" />
                                                <asp:Button ID="btnCollapseAll" runat="server" Text="Collapse All" CssClass="btn btn-xs btn-default" OnClick="btnCollapseAll_Click" />

                                                <button type="button" class="btn btn-xs btn-primary ml-2" onclick="checkAllNodes(true)">Check All</button>
                                                <button type="button" class="btn btn-xs btn-secondary" onclick="checkAllNodes(false)">Uncheck All</button>
                                            </div>
                                        </div>

                                        <asp:TreeView ID="tvControls" runat="server" ShowCheckBoxes="All"
                                            ShowLines="true" OnSelectedNodeChanged="tvControls_SelectedNodeChanged">
                                            <NodeStyle ForeColor="#73879C" Font-Size="14px" NodeSpacing="2px" />
                                            <SelectedNodeStyle BackColor="#dff0d8" Font-Bold="true" />
                                        </asp:TreeView>
                                    </div>
                                </div>

                                <div class="wizard-actions">
                                    <button type="button" class="btn btn-secondary" onclick="goToStep(1)"><i class="fa fa-arrow-left"></i>Back</button>
                                    <button type="button" class="btn btn-primary" onclick="goToStep(3)">Next Step <i class="fa fa-arrow-right"></i></button>
                                </div>
                            </div>

                            <div id="step-panel-3" class="step-panel">
                                <h3 class="font-weight-bold mb-3 text-primary"><i class="fa fa-lock"></i>Step 3: Assign Security Permission</h3>
                                <p class="text-muted">Select the permission level required to interact with the checked control(s).</p>

                                <div class="alert alert-info">
                                    <i class="fa fa-info-circle"></i>
                                    <strong>Current Status: </strong>
                                    <asp:Label ID="lblMappingStatus" runat="server" Text="Click a control name in Step 2 to view its mapping."></asp:Label>
                                </div>

                                <div class="form-group mt-3">
                                    <label class="font-weight-bold">Required Permission: <span class="text-danger">*</span></label>
                                    <asp:DropDownList ID="ddlPermissions" runat="server" CssClass="form-control mb-3" />
                                </div>

                                <div class="form-group font-weight-bold mt-2">
                                    <asp:Label ID="lblMessage" runat="server" Text=""></asp:Label>
                                </div>

                                <div class="wizard-actions">
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="goToStep(2)"><i class="fa fa-arrow-left"></i>Back</button>
                                        <button type="button" class="btn btn-default" onclick="goToStep(1)"><i class="fa fa-refresh"></i>Start Over</button>
                                    </div>
                                    <asp:Button ID="btnMap" runat="server" Text="Save Bulk Mapping" CssClass="btn btn-success" OnClick="btnMap_Click" />
                                </div>
                            </div>

                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function updateWizardUI() {
            var step = parseInt(document.getElementById('<%= hfStep.ClientID %>').value) || 1;

            $('.wizard-steps li').removeClass('active completed');
            $('.step-panel').removeClass('active');

            for (var i = 1; i < step; i++) {
                $('#step-head-' + i).addClass('completed');
            }
            $('#step-head-' + step).addClass('active');
            $('#step-panel-' + step).addClass('active');
        }

        function expandAll() { __doPostBack('<%= tvControls.UniqueID %>', 'ExpandAll'); }
        function collapseAll() { __doPostBack('<%= tvControls.UniqueID %>', 'CollapseAll'); }

        function checkAllNodes(checkState) {
            $('#<%= tvControls.ClientID %> input[type="checkbox"]').prop('checked', checkState);
        }

        function goToStep(stepNumber) {
            document.getElementById('<%= hfStep.ClientID %>').value = stepNumber;
            updateWizardUI();
        }

        $(document).ready(function () {
            updateWizardUI();
        });
    </script>
</asp:Content>
