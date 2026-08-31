<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeCardControl.ascx.cs" Inherits="AnmolDristi.Controls.EmployeeCardControl" %>
<div id="divInternalEmpError" class="internal-emp-error vms-emp-error" style="display: none;" role="alert" aria-live="assertive"></div>
<div id="divInternalEmpConfirm" class="internal-emp-card vms-emp-card" style="display: none;" role="region" aria-label="Employee confirmation">
    <h5>Confirm your details</h5>
    <p class="internal-emp-confirmed" id="lblInternalEmpConfirmed" style="display: none;"><i class="fa fa-check-circle" aria-hidden="true"></i> Confirmed — you can continue.</p>
    <dl>
        <dt>Employee Code</dt>
        <dd id="lblEmpConfirmCode"></dd>
        <dt>Name</dt>
        <dd id="lblEmpConfirmName"></dd>
        <dt>Department</dt>
        <dd id="lblEmpConfirmDept"></dd>
        <dt>Designation</dt>
        <dd id="lblEmpConfirmDesig"></dd>
        <dt>Email</dt>
        <dd id="lblEmpConfirmEmail"></dd>
        <dt>Mobile (HR record)</dt>
        <dd id="lblEmpConfirmMobile"></dd>
    </dl>
    <button type="button" id="btnConfirmInternalEmp" class="btn btn-success btn-sm">Yes, this is me</button>
    <button type="button" id="btnChangeInternalEmp" class="btn btn-default btn-sm">Change code</button>
</div>
