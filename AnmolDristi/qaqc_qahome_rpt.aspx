<%@ Page Title="QA Dashboard" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qahome_rpt.aspx.cs" Inherits="AnmolDristi.qaqc_qahome_rpt" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .tile-link {
            display: block;
            text-decoration: none;
            color: inherit;
            height: 100%; /* Ensures the link takes up the full height of the row */
        }

        .tile-link:hover .tile-stats {
            background-color: #f9f9f9;
            transition: 0.3s;
        }

        .tile-stats {
            padding: 15px;
            border: 1px solid #E4E4E4;
            border-radius: 6px;
            background: #fff;
            height: 100%; /* Uniform tile heights */
        }

        .count {
            font-size: 28px;
            font-weight: bold;
        }

        .chart-container {
            position: relative;
            height: 65px; 
            width: 100%;
            margin-top: 15px;
        }

        .text-success { color: #1ABB9C; }
        .text-danger { color: #E74C3C; }
        .text-muted { color: #73879C; }
        
        .row.top_tiles {
            display: flex;
            flex-wrap: wrap; 
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hfDayLabels" runat="server" />
    <asp:HiddenField ID="hfFullDates" runat="server" />
    
    <div class="right_col" role="main">
        
        <div class="row top_tiles">
            
            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="Process_Report.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblProcessChecking" runat="server" /></div>
                        <h3>Process Checking</h3>
                        <asp:Label ID="lblProcessTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartProcess"></canvas></div>
                        <asp:HiddenField ID="hfProcessData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="qaqc_qcinspector_rpt_.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblQCInspector" runat="server" /></div>
                        <h3>Inspector Report</h3>
                        <asp:Label ID="lblQCInspectorTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartQC"></canvas></div>
                        <asp:HiddenField ID="hfQCData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="qaqc_packet_weight_view.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblPacketWeights" runat="server" /></div>
                        <h3>Packet Weights</h3>
                        <asp:Label ID="lblPacketTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartPacket"></canvas></div>
                        <asp:HiddenField ID="hfPacketData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="qaqc_rotaryline_view.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblRotaryLine" runat="server" /></div>
                        <h3>Line Weight</h3>
                        <asp:Label ID="lblRotaryTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartRotary"></canvas></div>
                        <asp:HiddenField ID="hfRotaryData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="vm_ccp_checklist.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblCCPChecklist" runat="server" /></div>
                        <h3>CCP Checklist</h3>
                        <asp:Label ID="lblCCPTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartCCP"></canvas></div>
                        <asp:HiddenField ID="hfCCPData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="vm_leak_test.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblLeakSeal" runat="server" /></div>
                        <h3>Leak/Slanted</h3>
                        <asp:Label ID="lblLeakTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartLeak"></canvas></div>
                        <asp:HiddenField ID="hfLeakData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="preDispatchVM.aspx" class="tile-link">
                    <div class="tile-stats">
                        <div class="count"><asp:Label ID="lblPreDispatch" runat="server" /></div>
                        <h3>Pre-Dispatch</h3>
                        <asp:Label ID="lblDispatchTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartDispatch"></canvas></div>
                        <asp:HiddenField ID="hfDispatchData" runat="server" />
                    </div>
                </a>
            </div>

            <div class="col-lg-3 col-md-3 col-sm-6 col-xs-12" style="margin-bottom: 20px;">
                <a href="Critical_Incident_ViewPage.aspx" class="tile-link">
                    <div class="tile-stats" id="tileCriticalIncident" runat="server">
                        <div class="count"><asp:Label ID="lblCriticalIncident" runat="server" /></div>
                        <h3>Critical Incident</h3>
                        <asp:Label ID="lblCriticalTrend" runat="server" />
                        <div class="chart-container"><canvas id="chartCritical"></canvas></div>
                        <asp:HiddenField ID="hfCriticalData" runat="server" />
                    </div>
                </a>
            </div>
            
        </div> 

        <div class="row">
            <div class="col-md-12" id="Div2" runat="server" visible="true">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>Quality Assurance Analytics</h2>
                        <ul class="nav navbar-right panel_toolbox">
                            <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                        </ul>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <a class="btn btn-app" href="qaqc_qci_analtics.aspx">
                            <span class="badge bg-green">
                                <asp:Label ID="Label2" runat="server" Text="0" Font-Bold="true"></asp:Label>
                            </span>
                            <i class="fa fa-bar-chart"></i> QCI Graph
                        </a>
                        <a class="btn btn-app" href="qaqc_pcr_analytics.aspx">
                            <span class="badge bg-green">
                                <asp:Label ID="Label1" runat="server" Text="0" Font-Bold="true"></asp:Label>
                            </span>
                            <i class="fa fa-pie-chart"></i> PCR Graph
                        </a>
                    </div>
                </div>
            </div>
        </div>

    </div>

    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2"></script>
    <script type="text/javascript">
        
        function getStatusColor(trendLabelId) {
            const labelElement = document.getElementById(trendLabelId);
            if (!labelElement) return '#73879C'; 
            
            const htmlStr = labelElement.innerHTML;
            if (htmlStr.includes('text-success')) return '#1ABB9C'; 
            if (htmlStr.includes('text-danger')) return '#E74C3C';  
            
            return '#73879C'; 
        }

        function renderMiniChart(canvasId, dataString, color, labels, fullDates) {
            if (!dataString) return;
            const canvas = document.getElementById(canvasId);
            if (!canvas) return;

            const ctx = canvas.getContext('2d');
            const data = dataString.split(',').map(Number);
            
            const maxVal = Math.max(...data);
            const suggestedMax = maxVal === 0 ? 10 : maxVal + (maxVal * 0.4);

            new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        data: data,
                        backgroundColor: color, 
                        borderRadius: 3,
                        barPercentage: 0.7
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                title: function (ctx) {
                                    if (fullDates && fullDates.length > 0) {
                                        return fullDates[ctx[0].dataIndex]; 
                                    }
                                    return ctx[0].label; 
                                },
                                label: function (ctx) { 
                                    return `Submissions: ${ctx.raw}`; 
                                }
                            }
                        },
                        datalabels: {
                            anchor: 'end',
                            align: function (ctx) {
                                return ctx.dataset.data[ctx.dataIndex] < (maxVal * 0.15) ? 'end' : 'top';
                            },
                            color: '#73879C',
                            font: { size: 10, weight: 'bold' },
                            formatter: function (value) { return value > 0 ? value : ''; }
                        }
                    },
                    scales: {
                        x: { display: false }, 
                        y: {
                            display: false,
                            beginAtZero: true,
                            suggestedMax: suggestedMax
                        }
                    },
                    interaction: { mode: 'index', intersect: false }
                },
                plugins: [ChartDataLabels]
            });
        }

        function initCharts() {
            if (typeof Chart === 'undefined') {
                setTimeout(initCharts, 50);
                return;
            }

            const labelString = document.getElementById('<%= hfDayLabels.ClientID %>').value;
            const fullDateString = document.getElementById('<%= hfFullDates.ClientID %>').value; 
            
            if (!labelString) return;
            
            const labels = labelString.split(',');
            const fullDates = fullDateString ? fullDateString.split('|') : labels; 

            renderMiniChart('chartProcess', document.getElementById('<%= hfProcessData.ClientID %>').value, getStatusColor('<%= lblProcessTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartQC', document.getElementById('<%= hfQCData.ClientID %>').value, getStatusColor('<%= lblQCInspectorTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartPacket', document.getElementById('<%= hfPacketData.ClientID %>').value, getStatusColor('<%= lblPacketTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartRotary', document.getElementById('<%= hfRotaryData.ClientID %>').value, getStatusColor('<%= lblRotaryTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartCCP', document.getElementById('<%= hfCCPData.ClientID %>').value, getStatusColor('<%= lblCCPTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartLeak', document.getElementById('<%= hfLeakData.ClientID %>').value, getStatusColor('<%= lblLeakTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartDispatch', document.getElementById('<%= hfDispatchData.ClientID %>').value, getStatusColor('<%= lblDispatchTrend.ClientID %>'), labels, fullDates);
            renderMiniChart('chartCritical', document.getElementById('<%= hfCriticalData.ClientID %>').value, getStatusColor('<%= lblCriticalTrend.ClientID %>'), labels, fullDates);
        }

        window.addEventListener("load", initCharts);
    </script>
</asp:Content>