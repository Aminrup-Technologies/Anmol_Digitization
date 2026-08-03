# HRMS Import Module — Technical Documentation

Read-only reverse-engineering of `HrmsDataImport` and `HrmsBatchManager` for AnmolDristi ERP.

**No application or SQL code was modified for this documentation set.**

| Doc | Contents |
|---|---|
| [01_Project_Overview.md](01_Project_Overview.md) | Purpose, actors, dependencies, entry/exit, workflow |
| [02_UI_Flow.md](02_UI_Flow.md) | Screens, controls, navigation, journeys |
| [03_Page_Event_Flow.md](03_Page_Event_Flow.md) | Page_Load, buttons, JS, UpdatePanels, validation order |
| [04_CodeBehind_Analysis.md](04_CodeBehind_Analysis.md) | Per-method analysis, dead/duplicate code |
| [05_SQL_Dependency_Map.md](05_SQL_Dependency_Map.md) | Tables/SPs referenced; dependency tree |
| [06_Table_CRUD_Matrix.md](06_Table_CRUD_Matrix.md) | CRUD by page/method |
| [07_Business_Rules.md](07_Business_Rules.md) | Rules found in application code only |
| [08_Data_Flow.md](08_Data_Flow.md) | Upload→staging→sync; tracked variables |
| [09_StoredProcedure_Review.md](09_StoredProcedure_Review.md) | SP call-site comparison (scripts absent) |
| [10_Risk_Assessment.md](10_Risk_Assessment.md) | Critical→Low risks |
| [11_Refactoring_Backlog.md](11_Refactoring_Backlog.md) | Backlog only — no refactoring performed |
| [12_Technical_Debt.md](12_Technical_Debt.md) | Smells, magic strings, potential bugs |

Supplementary earlier single-file summary: [HRMS_Import_Module_Analysis.md](HRMS_Import_Module_Analysis.md).

**Important gap:** `usp_RunHRMSSynchronization`, `ImportBatchMaster`, `Staging_EmployeeData`, and `MST_HRMSImportField` have no SQL scripts in this repository. Statements that cannot be proven from sources are marked *Not determinable from current repository.*
