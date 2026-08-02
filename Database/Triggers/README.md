# Triggers

No triggers exist on any table used by the Monthly Sales Claims workflow.

This determination was made by querying `sys.triggers` with `parent_id` set to each of the sixteen tables listed under `Tables/` (see `Inventory.md`), which is an exhaustive, catalog-level check — not a name-pattern search. Zero rows were returned for every table.

One trigger exists elsewhere in the database (`TRG_FormsApprovalMatrix_AutoVersion`, on `MST_FormsApprovalMatrix`), but that table belongs to the QA/QC Forms Approval Matrix module, not Monthly Sales Claims, and is out of scope for this extraction.

This folder is retained empty, per the requested folder structure, rather than populated with an invented placeholder trigger.

See `ExtractionReport.md` for the full extraction methodology.
