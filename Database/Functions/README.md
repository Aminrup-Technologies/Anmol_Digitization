# Functions

No user-defined scalar functions, table-valued functions, or aggregate functions were located that are used by, or referenced from, the Monthly Sales Claims workflow.

This determination was made by:

1. Searching all Monthly Sales Claims application source files (`.aspx.cs`, `.aspx`) for function invocations.
2. Traversing `sys.sql_expression_dependencies` for every stored procedure confirmed to belong to the workflow (see `Inventory.md`), to detect any function referenced only from within procedure bodies.
3. Searching `sys.objects` (types `FN`, `TF`, `IF`, `FS`, `FT`) database-wide for any object whose name suggests a connection to the Sales/Approval/Claim/Expense domain.

No object of this kind was found. This folder is retained empty, per the requested folder structure, rather than populated with an invented placeholder function.

See `ExtractionReport.md` for the full extraction methodology.
