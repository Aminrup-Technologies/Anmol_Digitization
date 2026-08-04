<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="HrmsSyncInfoPanel.ascx.cs" Inherits="AnmolDristi.Controls.HrmsSyncInfoPanel" %>

<%--
  Reusable Synchronization Information slide-out panel (v1.0).
  Static content — safe to include on other admin pages.
  Open via: elements with data-hrms-help-trigger + data-hrms-help-section
  Or: window.HrmsSyncInfoPanel.open('hrms-help-section-modes')
--%>

<div id="hrmsSyncInfoBackdrop"
     class="hrms-sync-info-backdrop"
     data-hrms-help-close="1"
     aria-hidden="true"></div>

<aside id="hrmsSyncInfoPanel"
       class="hrms-sync-info-panel"
       role="dialog"
       aria-modal="true"
       aria-labelledby="hrmsSyncInfoTitle"
       aria-hidden="true">

    <header class="hrms-sync-info-header">
        <h3 id="hrmsSyncInfoTitle">Synchronization Information</h3>
        <button type="button"
                class="hrms-sync-info-close"
                data-hrms-help-close="1"
                aria-label="Close synchronization information">
            <i class="fa fa-times" aria-hidden="true"></i>
        </button>
    </header>

    <div class="hrms-sync-info-body">

        <!-- SECTION 1 -->
        <section id="hrms-help-section-modes" class="hrms-sync-info-section" tabindex="-1">
            <h4>1. Processing Modes</h4>

            <div class="mode-block">
                <span class="mode-title">Insert New Employees Only</span>
                <p>Only employees not already present in <strong>MST_UserMaster</strong> are inserted. Existing employees are left unchanged.</p>
            </div>

            <div class="mode-block">
                <span class="mode-title">Update All Existing Employees</span>
                <p>Updates all supported fields of existing employees when values have changed.</p>
            </div>

            <div class="mode-block">
                <span class="mode-title">Update Selected Fields Only</span>
                <p>Updates only the fields checked under <strong>Fields to Overwrite</strong>. Unchecked fields remain unchanged.</p>
            </div>
        </section>

        <!-- SECTION 2 -->
        <section id="hrms-help-section-modules" class="hrms-sync-info-section" tabindex="-1">
            <h4>2. Synchronization Modules</h4>

            <div class="module-block">
                <span class="module-title">Synchronize Masters</span>
                <p>Inserts / updates master reference data:</p>
                <ul>
                    <li>MST_Company</li>
                    <li>MST_Region</li>
                    <li>MST_Division</li>
                    <li>MST_Category</li>
                    <li>MST_Plant</li>
                    <li>MST_Branch</li>
                    <li>MST_Department</li>
                    <li>MST_SubDepartment</li>
                    <li>MST_Grade</li>
                    <li>MST_Designation</li>
                </ul>
            </div>

            <div class="module-block">
                <span class="module-title">Synchronize Employees</span>
                <p>Updates <strong>MST_UserMaster</strong>:</p>
                <ul>
                    <li>Insert new employees</li>
                    <li>Update existing employees</li>
                </ul>
                <div class="hrms-code-chain">
                    Designation_ID is resolved automatically:
                    <span>HRMS Business Code</span>
                    <span class="arrow">↓</span>
                    <span>ERP Identity (MST_Designation.Id → MST_UserMaster.Designation_ID)</span>
                </div>
            </div>

            <div class="module-block">
                <span class="module-title">Synchronize Reporting Hierarchy</span>
                <p>Updates hierarchy columns on <strong>MST_UserMaster</strong> only:</p>
                <ul>
                    <li>ReportingManager</li>
                    <li>ReportingManagerCode</li>
                    <li>ReportingManagersManagerName</li>
                    <li>ReportingManagersManagerCode</li>
                </ul>
            </div>
        </section>

        <!-- SECTION 3 -->
        <section id="hrms-help-section-overwrite" class="hrms-sync-info-section" tabindex="-1">
            <h4>3. Fields to Overwrite</h4>
            <p>When <strong>Update Selected Fields Only</strong> is selected, only the checked fields are updated on existing employees.</p>
            <p>Unchecked fields remain unchanged in <strong>MST_UserMaster</strong>.</p>
            <p class="hrms-sync-info-footnote">This section is hidden for Insert-only and Update-all modes.</p>
        </section>

        <!-- SECTION 4 -->
        <section id="hrms-help-section-flow" class="hrms-sync-info-section" tabindex="-1">
            <h4>4. Execution Flow</h4>
            <ol class="hrms-exec-flow">
                <li><span class="step-num">1</span>Upload CSV</li>
                <li><span class="step-num">2</span>Validation</li>
                <li><span class="step-num">3</span>Synchronize Masters</li>
                <li><span class="step-num">4</span>Insert New Employees</li>
                <li><span class="step-num">5</span>Update Existing Employees</li>
                <li><span class="step-num">6</span>Update Reporting Hierarchy</li>
                <li><span class="step-num">7</span>Batch Completed</li>
            </ol>
            <p class="hrms-sync-info-footnote">Steps 3–6 run only for the modules you select, subject to Processing Mode.</p>
        </section>

        <!-- SECTION 5 -->
        <section id="hrms-help-section-validation" class="hrms-sync-info-section" tabindex="-1">
            <h4>5. Validation</h4>
            <p>Validation runs <strong>before</strong> synchronization. If validation fails, synchronization is not executed.</p>
            <p>Checks include:</p>
            <ul>
                <li>Mandatory fields</li>
                <li>Duplicate Employee Codes</li>
                <li>Invalid Master references</li>
                <li>Missing Reporting Managers</li>
                <li>Invalid Processing Mode</li>
                <li>Invalid Modules</li>
            </ul>
        </section>

        <!-- SECTION 6 -->
        <section id="hrms-help-section-warnings" class="hrms-sync-info-section" tabindex="-1">
            <h4>6. Warnings vs Errors</h4>

            <div class="hrms-callout hrms-callout-warning">
                <strong><i class="fa fa-exclamation-triangle" aria-hidden="true"></i> Warning</strong>
                <ul>
                    <li>Synchronization continues.</li>
                    <li>Row becomes <strong>Processed</strong>.</li>
                    <li>Warning text is preserved on the staging row.</li>
                </ul>
            </div>

            <div class="hrms-callout hrms-callout-error">
                <strong><i class="fa fa-times-circle" aria-hidden="true"></i> Error</strong>
                <ul>
                    <li>Synchronization stops for that row.</li>
                    <li>Row becomes <strong>Error</strong>.</li>
                </ul>
            </div>
        </section>

        <!-- SECTION 7 -->
        <section id="hrms-help-section-status" class="hrms-sync-info-section" tabindex="-1">
            <h4>7. Batch Status Lifecycle</h4>
            <ul class="hrms-status-flow">
                <li>Uploaded</li>
                <li>Validated</li>
                <li>Processing</li>
            </ul>
            <div class="hrms-status-branch">
                <span>→ Processed</span>
                <span>or</span>
                <span>→ Failed</span>
            </div>
        </section>

    </div>
</aside>
