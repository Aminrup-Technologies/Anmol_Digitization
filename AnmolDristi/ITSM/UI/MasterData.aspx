<%-- ========================================================================
     File:    MasterData.aspx
     Layer:   UI — Presentation
     When:    2026-08-25
     What:    Master Data Management landing page. Provides navigation to
              individual master-data modules. Currently shows placeholders
              for modules whose CRUD pages are not yet implemented.
     Why:     ITSM Phase 1B — MDM navigation and authorization foundation.
              All CRUD is deferred to subsequent PRs.
     ======================================================================== --%>
<%@ Page Title="ITSM | Master Data" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="MasterData.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.MasterData" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .mdm-header { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; margin-bottom: 15px; }
        .mdm-card { border: 1px solid #e6e9ed; border-radius: 4px; padding: 20px; margin-bottom: 15px; background: #fff; transition: box-shadow 0.2s; }
        .mdm-card:hover { box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
        .mdm-card h4 { margin: 0 0 6px 0; font-size: 15px; color: #333; }
        .mdm-card p { margin: 0; font-size: 13px; color: #73879c; }
        .mdm-card .badge-coming-soon { display: inline-block; padding: 2px 8px; border-radius: 3px; font-size: 11px; font-weight: 600; background: #ffc107; color: #333; margin-top: 8px; }
        .mdm-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 15px; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <div class="mdm-header">
                            <h2><i class="fa fa-database"></i> ITSM Master Data Management</h2>
                        </div>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 10px;"></asp:Label>

                        <div class="mdm-grid">
                            <%-- Category --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-folder-open"></i> Category</h4>
                                <p>Top-level ticket taxonomy (Hardware, Software, Network).</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- SubCategory --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-folder"></i> Sub-Category</h4>
                                <p>Second-tier taxonomy under Category (Printer, ERP Login).</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- Item --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-cube"></i> Item</h4>
                                <p>Third-tier taxonomy under Sub-Category (HP LaserJet Pro).</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- Impact --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-bolt"></i> Impact Levels</h4>
                                <p>Business impact classification (Low, Medium, High).</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- Urgency --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-clock-o"></i> Urgency Levels</h4>
                                <p>Response urgency classification (Low, Medium, High).</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- Priority (read-only) --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-flag"></i> Priority Levels</h4>
                                <p>Auto-calculated priority tiers (P1–P4) with SLA hours.</p>
                                <span class="badge-coming-soon">Read-Only</span>
                            </div>

                            <%-- Priority Matrix --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-th"></i> Priority Matrix</h4>
                                <p>Impact × Urgency → Priority mapping configuration.</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- Request Type --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-tags"></i> Request Types</h4>
                                <p>Ticket families and numbering prefixes (IR, SR, CR).</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>

                            <%-- Support Mapping --%>
                            <div class="mdm-card">
                                <h4><i class="fa fa-sitemap"></i> Support Mappings</h4>
                                <p>Branch + taxonomy → default support agent dispatch rules.</p>
                                <span class="badge-coming-soon">Coming Soon</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
