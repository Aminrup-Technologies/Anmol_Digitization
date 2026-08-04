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

        <!-- TECHNICAL DETAILS (Administrator Reference) — collapsed by default -->
        <section id="hrms-help-section-technical" class="hrms-sync-info-section hrms-tech-section" tabindex="-1">
            <button type="button"
                    id="hrmsTechDetailsToggle"
                    class="hrms-tech-toggle btn btn-block"
                    data-toggle="collapse"
                    data-target="#hrmsTechDetailsCollapse"
                    aria-expanded="false"
                    aria-controls="hrmsTechDetailsCollapse">
                <span class="hrms-tech-toggle-label">
                    <i class="fa fa-cogs" aria-hidden="true"></i>
                    Technical Details
                    <span class="badge badge-primary">Administrator Reference</span>
                </span>
                <i class="fa fa-chevron-down hrms-tech-chevron" aria-hidden="true"></i>
            </button>

            <div id="hrmsTechDetailsCollapse" class="collapse hrms-tech-collapse">
                <p class="hrms-tech-intro">
                    Reference for ERP Administrators, Developers, and Support Engineers.
                    Informational only — does not change runtime behaviour.
                </p>

                <!-- TD §1 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-sitemap" aria-hidden="true"></i>
                        Stored Procedure Execution Flow
                        <span class="badge badge-info">Flow</span>
                    </div>
                    <div class="card-body">
                        <ol class="hrms-tech-timeline">
                            <li><code>Validation</code> <span class="text-muted">(UI gate)</span></li>
                            <li><code>dbo.usp_HSE_ValidateBatch</code></li>
                            <li><code>dbo.usp_RunHRMSSynchronization</code></li>
                            <li><code>dbo.usp_HSE_SynchronizeMasters</code></li>
                            <li><code>dbo.usp_HSE_InsertNewEmployees</code></li>
                            <li><code>dbo.usp_HSE_UpdateChangedEmployees</code></li>
                            <li><code>dbo.usp_HSE_UpdateReportingHierarchy</code></li>
                        </ol>
                    </div>
                </div>

                <!-- TD §2 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-table" aria-hidden="true"></i>
                        Synchronization Modules Mapping
                        <span class="badge badge-success">Tables</span>
                    </div>
                    <div class="card-body">
                        <div class="hrms-tech-map">
                            <div class="hrms-tech-map-title">Synchronize Masters</div>
                            <div class="hrms-tech-map-arrow">↓ Affected Tables</div>
                            <ul class="hrms-tech-table-list">
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
                        <div class="hrms-tech-map">
                            <div class="hrms-tech-map-title">Synchronize Employees</div>
                            <div class="hrms-tech-map-arrow">↓ Affected Table</div>
                            <ul class="hrms-tech-table-list">
                                <li>MST_UserMaster</li>
                            </ul>
                        </div>
                        <div class="hrms-tech-map">
                            <div class="hrms-tech-map-title">Synchronize Reporting Hierarchy</div>
                            <div class="hrms-tech-map-arrow">↓ Affected Table</div>
                            <ul class="hrms-tech-table-list">
                                <li>MST_UserMaster</li>
                            </ul>
                        </div>
                    </div>
                </div>

                <!-- TD §3 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-link" aria-hidden="true"></i>
                        Designation Resolution
                        <span class="badge badge-warning">Important</span>
                    </div>
                    <div class="card-body">
                        <p>HRMS exports <code>Designation_ID</code> as a <strong>Business Code</strong>.</p>
                        <p>Synchronization automatically resolves:</p>
                        <ol class="hrms-tech-timeline hrms-tech-timeline-compact">
                            <li><code>Staging.Designation_ID</code></li>
                            <li><code>MST_Designation.Designation_ID</code></li>
                            <li><code>MST_Designation.Id</code></li>
                            <li><code>MST_UserMaster.Designation_ID</code></li>
                        </ol>
                        <p class="mb-0">This prevents Business Codes from being stored inside <code>MST_UserMaster</code>.</p>
                    </div>
                </div>

                <!-- TD §4 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-sliders" aria-hidden="true"></i>
                        Processing Modes
                        <span class="badge badge-primary">Modes</span>
                    </div>
                    <div class="card-body">
                        <div class="hrms-tech-mode">
                            <span class="badge badge-dark">Mode 0</span>
                            <strong>Insert New Employees Only</strong>
                            <ul>
                                <li><code>AllowOverwrite = False</code></li>
                            </ul>
                        </div>
                        <div class="hrms-tech-mode">
                            <span class="badge badge-dark">Mode 1</span>
                            <strong>Update All Existing Employees</strong>
                            <ul>
                                <li><code>AllowOverwrite = True</code></li>
                                <li><code>OverwriteFields</code> ignored</li>
                            </ul>
                        </div>
                        <div class="hrms-tech-mode">
                            <span class="badge badge-dark">Mode 2</span>
                            <strong>Update Selected Fields Only</strong>
                            <ul>
                                <li><code>AllowOverwrite = True</code></li>
                                <li>Uses selected <code>OverwriteFields</code> list</li>
                            </ul>
                        </div>
                    </div>
                </div>

                <!-- TD §5 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-list-alt" aria-hidden="true"></i>
                        Overwrite Fields Parameter
                        <span class="badge badge-info">Parameter</span>
                    </div>
                    <div class="card-body">
                        <p>Selected fields are converted into <code>FieldCode</code> values.</p>
                        <p>Example:</p>
                        <pre class="hrms-tech-code">EmployeeName|Email|Designation|Branch|Region</pre>
                        <p class="mb-0">This becomes <code>@OverwriteFields</code> passed to <code>usp_RunHRMSSynchronization</code>.</p>
                    </div>
                </div>

                <!-- TD §6 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-check-square-o" aria-hidden="true"></i>
                        Validation Coverage
                        <span class="badge badge-success">Checks</span>
                    </div>
                    <div class="card-body">
                        <ul class="hrms-tech-checklist">
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Mandatory fields</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Duplicate Employee Codes</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Duplicate HRMS records</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Invalid Master references</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Invalid Reporting Managers</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Invalid Modules</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Invalid Processing Mode</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Missing Batch</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Empty Batch</li>
                        </ul>
                    </div>
                </div>

                <!-- TD §7 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-exchange" aria-hidden="true"></i>
                        Status Lifecycle
                        <span class="badge badge-warning">Status</span>
                    </div>
                    <div class="card-body">
                        <div class="hrms-tech-map">
                            <div class="hrms-tech-map-title"><code>ImportBatchMaster</code></div>
                            <ol class="hrms-tech-timeline hrms-tech-timeline-compact">
                                <li>Uploaded</li>
                                <li>Validated</li>
                                <li>Processing</li>
                                <li>Processed <span class="text-muted">or</span> Failed</li>
                            </ol>
                        </div>
                        <div class="hrms-tech-map">
                            <div class="hrms-tech-map-title"><code>Staging_EmployeeData</code></div>
                            <ol class="hrms-tech-timeline hrms-tech-timeline-compact">
                                <li>Pending</li>
                                <li>Processed <span class="text-muted">or</span> Error</li>
                            </ol>
                            <p class="mb-0 hrms-sync-info-footnote">Warnings remain <strong>Processed</strong> (warning text preserved).</p>
                        </div>
                    </div>
                </div>

                <!-- TD §8 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-tachometer" aria-hidden="true"></i>
                        Performance Notes
                        <span class="badge badge-info">Perf</span>
                    </div>
                    <div class="card-body">
                        <ul class="hrms-tech-checklist">
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Batch based processing</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Bulk Insert into Staging</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Modular synchronization</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Single transaction orchestration</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> Nested transaction safe</li>
                            <li><i class="fa fa-check text-success" aria-hidden="true"></i> <code>XACT_STATE</code> protected rollback</li>
                        </ul>
                    </div>
                </div>

                <!-- TD §9 -->
                <div class="card hrms-tech-card">
                    <div class="card-header">
                        <i class="fa fa-info-circle" aria-hidden="true"></i>
                        Version Information
                        <span class="badge badge-primary">v1.0</span>
                    </div>
                    <div class="card-body">
                        <table class="table table-sm hrms-tech-version-table mb-0">
                            <tbody>
                                <tr>
                                    <th scope="row">Engine</th>
                                    <td>HRMS Synchronization Engine</td>
                                </tr>
                                <tr>
                                    <th scope="row">Version</th>
                                    <td><span class="badge badge-primary">1.0</span></td>
                                </tr>
                                <tr>
                                    <th scope="row">Architecture</th>
                                    <td>Modular</td>
                                </tr>
                                <tr>
                                    <th scope="row">Status</th>
                                    <td><span class="badge badge-success">Production Ready</span></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <footer class="hrms-tech-footer">
                    <div class="hrms-tech-footer-rule"></div>
                    <div class="hrms-tech-footer-title">HRMS Synchronization Engine</div>
                    <dl class="hrms-tech-footer-meta">
                        <dt>Version</dt><dd>1.0</dd>
                        <dt>Build</dt><dd>2026.08</dd>
                        <dt>Architecture</dt><dd>Modular Synchronization Engine</dd>
                        <dt>Last Updated</dt><dd>August 2026</dd>
                    </dl>
                    <div class="hrms-tech-footer-copy">© Anmol Digitization – HRMS Integration Module</div>
                </footer>
            </div>
        </section>

    </div>
</aside>
