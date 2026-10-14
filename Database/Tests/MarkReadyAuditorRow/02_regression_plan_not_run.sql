/*
    Regression plan for the Mark Ready Auditor-row package.
    Authoring status: NOT RUN.

    This file prints the plan and returns. It does not connect, query,
    call dbo.usp_MarkRequestReadyForPayment, or change data.
    State-changing cases stay NOT RUN until a separate authorization
    names a disposable fixture. Requests 100 and 121 are not that fixture.

    Payment disbursement is not part of this plan and has not been tested.
*/
SET NOCOUNT ON;

PRINT N'NOT RUN | Mark Ready Auditor-row regression plan. No test was executed.';
PRINT N'1 | Normal Mark Ready on a Completed request with a Pending Auditor row.';
PRINT N'   Expected: that Auditor row becomes Approved, ActionBy and ActionDate are populated,';
PRINT N'   the header becomes ReadyForPayment, and one history row records Completed to ReadyForPayment';
PRINT N'   with remark Auditor marked Ready for Payment.';
PRINT N'   Do not use request 100 or 121. NOT RUN.';
PRINT N'2 | Repeated invocation after the header is ReadyForPayment.';
PRINT N'   Expected: the procedure raises Request not completed and leaves the header, history, and approver row unchanged.';
PRINT N'   NOT RUN.';
PRINT N'3 | Procedure error after EnsureHeaderCompletedForPayment has committed.';
PRINT N'   Current caller behavior: the Completed update and its history row commit in their own batch,';
PRINT N'   then the procedure runs without a transaction around both steps.';
PRINT N'   A procedure error can leave header Completed, the finalize history row present, and no ReadyForPayment history row.';
PRINT N'   Request 121 is not this case: history 797 and history 798 both exist.';
PRINT N'   Do not induce the failure. NOT RUN.';
PRINT N'4 | Amounts and expense or detail rows stay unchanged.';
PRINT N'   The reviewed procedure text does not reference those tables. A disposable before/after compare is still required.';
PRINT N'   NOT RUN.';
PRINT N'5 | Deployment target.';
PRINT N'   B_Apply_AuditorRowUpdate.sql allows MUMANMOAPP01\SQLEXPRESS / AnmolDristiDev and throws 58301 on AnmolDristi.';
PRINT N'   @Execute defaults to 0. The apply and rollback scripts were not executed.';
PRINT N'6 | Recovery idempotency.';
PRINT N'   R121 requires request 121, history 798, and approver 238 Pending with null action fields.';
PRINT N'   R100 requires request 100, history 702, and approver 196 under its own pins.';
PRINT N'   A matching post-state writes nothing. Any other state aborts. @Commit defaults to 0.';
PRINT N'   Neither recovery script was executed.';
RETURN;
