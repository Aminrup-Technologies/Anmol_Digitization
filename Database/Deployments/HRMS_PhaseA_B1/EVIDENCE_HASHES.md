# Evidence hashes — HRMS Phase A + B1

Captured 2026-09-02. Read-only extraction from `AnmolDristiDev` (UAT) and `AnmolDristi` (Live). Repository files at merge `5ff3a3f`.

SQL Server catalogs `CREATE OR ALTER PROCEDURE` as `CREATE   PROCEDURE` (spaces preserve source offsets). Therefore file bytes and `OBJECT_DEFINITION` bytes are not identical, even when the procedure logic is identical.

Definitions used here:

- **HASHBYTES SHA2_256** — `CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), m.definition)), 2)` from `sys.sql_modules`
- **Canonical body SHA-256 (UTF-8)** — text from the `CREATE` statement through the end, with `CREATE OR ALTER PROCEDURE` / `CREATE   PROCEDURE` unified to `CREATE PROCEDURE`, CRLF normalized to LF, trailing whitespace stripped, trailing `GO` removed

Canonical body SHA-256 matches between the repository file, the UAT catalog definition, and the generated deployment script body for every procedure below (`gen==repo==uat`).

## Expected HASHBYTES after CREATE OR ALTER (UAT actual)

`011_PostDeployment_Verification.sql` compares Live `OBJECT_DEFINITION` hashes to these UAT values.

| Object | UAT HASHBYTES SHA2_256 | UAT modify_date | ANSI_NULLS | QUOTED_IDENTIFIER |
|---|---|---|---|---|
| `usp_HSE_ValidateBatch` | `0C84CD55BF21274664390C6968A372EE9EA5702E64A13497F36EC321200698CD` | 2026-09-02 06:38:38 | ON | ON |
| `usp_RunHRMSSynchronization` | `2589DB21030252A0F0157741A630EA2EB9A45D0E545F4E0DC033F4F2CFCEB325` | 2026-09-02 06:39:03 | ON | ON |
| `usp_HSE_InsertNewEmployees` | `97FCB04920457A427DA5CDB8FCB1D5D0F28CC629666F7EB99078C6942A1A04D9` | 2026-09-02 06:39:30 | ON | ON |
| `usp_HSE_UpdateChangedEmployees` | `D67DAA1E717BDE279FD7E1DFC0A85008C984304EFF927B318A9FE51CFD6E024E` | 2026-09-02 06:39:59 | ON | ON |
| `usp_HSE_SynchronizeMasters` | `E64BC5EBC5A0C14B90BB939548032D98AE06CC730B24B63A87BA97E7FB296A3A` | 2026-08-04 07:58:40 | ON | ON |
| `usp_HSE_ReconcileValidationWarnings` | `694BE571F60C8825FF460C0C85E72BDB991B23B22AC721DDBCE926C79AE6EFEC` | 2026-08-04 12:02:57 | ON | ON |
| `usp_HSE_UpdateReportingHierarchy` | `5AF50C20DB061560558BB01DC6999B4B0C4B50121E7E92F0ADECFD732E7A4EF1` | 2026-08-03 20:57:17 | ON | ON |

## Canonical body SHA-256 (UTF-8) — repository = UAT = generated script body

| Object | Canonical SHA-256 |
|---|---|
| `usp_HSE_ValidateBatch` | `45DF9F6B0B6FA0FD90C01879B76FD6246E81DCF11B3B18449B42202B4AEEDA16` |
| `usp_RunHRMSSynchronization` | `04104392C62DD585F1731EF09344F922A7F6E973409044E057A9B0F25880379C` |
| `usp_HSE_InsertNewEmployees` | `B1AF35575A9B93B0B43DF003A5C35BD511B38E26207D079387B89349C713E698` |
| `usp_HSE_UpdateChangedEmployees` | `CEB335EF2C50EE0A335BC3E41194EDC6E911FE8089057D7C04B38D91EB4C671F` |
| `usp_HSE_SynchronizeMasters` | `286013A031031E8138309037A6FBDAA1D7B7ACD87A486066EB406F7FC6005290` |
| `usp_HSE_ReconcileValidationWarnings` | `D9FBE2808339B26E9C4A2D708858DA83D36ADEADB6927485CD54537CF5D8636A` |
| `usp_HSE_UpdateReportingHierarchy` | `0688DF3BF3F6B66EC2501B534936258F0D4CB5808A341374C3ED1CC828FFFF57` |

## Live HASHBYTES (current, before any deploy)

| Object | Live HASHBYTES SHA2_256 | Notes |
|---|---|---|
| `usp_RunHRMSSynchronization` | `89DD819B78A90677B69613EE5194F708E4A8692D9A4C4666E26FBB03826C16D2` | Stub, 552 bytes, `EXEC usp_ProcessEmployeeBatch` |
| `usp_ProcessEmployeeBatch` | `889329D34C00385D91BD7B094557C448490B033BB290081F404076F64D208958` | Same hash as UAT; **not** replaced by this package |
| Other HSE procedures | *(object missing)* | LIVE BEHIND |

## ValidateBatch XML compilation fix (UAT vs repository)

| Token | Repository | UAT |
|---|---|---|
| `DECLARE @ModuleXml XML` | Present | Present |
| `FROM @ModuleXml.nodes('/r/i')` | Present | Present |
| `DECLARE @ScopeXml XML` | Present | Present |
| `FROM @ScopeXml.nodes('/r/i')` | Present | Present |
| Inline `CONVERT(XML,…).nodes(` | Absent | Absent |
| `@Action` | Present | Present |
| `@OverwriteFields` | Present | Present |

This is commit `22405be` (Fix ValidateBatch module-count XML so the procedure compiles). UAT contains the corrected implementation. No additional UAT-only hotfix remains to merge.

## Generated SQL script file SHA-256 (UTF-8 bytes)

Markdown files in this folder are documentation and are not hashed here.

| File | SHA-256 |
|---|---|
| `001_PreDeployment_Check.sql` | `3C97DC72997F55A470AC416C42D82C3FCB755D8928A6AF0F1D793E56A709E6B9` |
| `002_ImportBatchMaster_MasterSyncColumns.sql` | `1FEA81553C3E4F133A58C229A1B0C9588513EFF0EC89C8D8DE83BB2E6682D2BD` |
| `003_Staging_EmployeeData_SubDepartment_Description.sql` | `08C0DF77B48CF47310EA32FDB2843E372B4445245170A9A568A55AD107C11BC7` |
| `004_usp_HSE_ValidateBatch.sql` | `D1FE834D83DBBB5212D5F08DBB511BB1BD8ACDC271DF378D228958483693327A` |
| `005_usp_HSE_SynchronizeMasters.sql` | `62499C0FAC03590F4743CD5FB7083327082265AB9818ED99ED9539FB1BBA2FDD` |
| `006_usp_HSE_ReconcileValidationWarnings.sql` | `E8453607312CDCFF0D0CEC81C5BEE36FA9E0C9C043472D14828541BAE1F20856` |
| `007_usp_HSE_InsertNewEmployees.sql` | `2A101D3B1AB530142A24845A77907242E29CD9A14A77AD8DFA88F142F7B719AD` |
| `008_usp_HSE_UpdateChangedEmployees.sql` | `A7B33908000122680510D81C0DC324703F2B71DB6A2E18BF1FBFF48CF650407B` |
| `009_usp_HSE_UpdateReportingHierarchy.sql` | `2F8A101EB36F35AE7E470B713E6149FFA725CBF0C8E65AAF23700A7BF9B5A31C` |
| `010_usp_RunHRMSSynchronization.sql` | `01EDAD78BBC731373FDA9881CF4605F3812D93950A4E1E28C746B79BDB7AF4CD` |
| `011_PostDeployment_Verification.sql` | `650EE88C7BA5FE0BAAD2F804ECDA4D3109BE7B826D8D9F74863D7A53B0AE09E8` |
