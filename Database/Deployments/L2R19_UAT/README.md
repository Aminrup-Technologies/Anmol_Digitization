# L2-R19 UAT package

UAT-only candidate for `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`. The live database `AnmolDristi` is forbidden. Nothing in this folder is authorized to run until the gates in `B_Apply_L2R19_Finalization_UAT.sql` and `D_Rollback_L2R19_Finalization_UAT.sql` are opened. Opening those gates is a later decision. This preparation did not execute SQL.

The execution record and the test matrix stay in `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md`. This file is the package order, not a second checklist.

## What changed in this package

The previous apply script deployed only `usp_MarkRequestReadyForPayment` and `usp_RecalculateApprovalRequest`, and its embedded Mark Ready text did not contain error `59112`. That body is withdrawn. Do not run a saved copy of it.

This package alters five procedures from the reviewed working-tree files, in one transaction:

1. `dbo.usp_RecalculateApprovalRequest`
2. `dbo.usp_ProcessApprovalAction`
3. `dbo.usp_UpdateSalesDailyExpenseStatus`
4. `dbo.usp_SubmitSalesDailyActivities`
5. `dbo.usp_MarkRequestReadyForPayment`

`dbo.usp_ForwardApprovalRequest` is a dependency. The apply reads its hash and rolls back if that hash changes. It is not part of the alter list.

No table or index change is in this package.

## Hashing

| Hash | Method |
| --- | --- |
| Source file SHA-256 | SHA-256 of the raw working-tree file bytes. |
| Catalog SHA-256 | `HASHBYTES('SHA2_256', CONVERT(varbinary(max), nvarchar_definition))`, which is SHA-256 over UTF-16LE with no BOM. `LEN` is the character count. |

Catalog text is the working-tree file with the procedure introducer normalized to `CREATE PROCEDURE`. `CREATE OR ALTER PROCEDURE` becomes `CREATE PROCEDURE`. The whitespace before `PROCEDURE` stays as it is in the file. The submitted batch uses `ALTER` in that same position. A 2026-10-11 RC014B apply on this server stored `ALTER PROCEDURE` as `CREATE PROCEDURE` and left the remaining characters unchanged. `B_Apply` commits only when `OBJECT_DEFINITION` equals that catalog text.

## Hashes

| Procedure | Source file SHA-256 | Catalog LEN | Catalog SHA-256 |
| --- | --- | --- | --- |
| `usp_UpdateSalesDailyExpenseStatus` | `EC79DABDF4D81223C28702965AF016F930702658B09A632933F3268F7B8DE9C5` | 12445 | `7C9007750F36C24F696B56EBC1AD7FC0A2CEC6441AD45A00815934D4FBC7E040` |
| `usp_ProcessApprovalAction` | `B8E2ADDCB0FDEFC3F8B46B643C65D888E90608059D1E06C17E76E5ECB4DA3059` | 42021 | `3E86754A9C605F752468C616D9923BD7B22F2396FE84363FAAB0E5CFBDA04EB9` |
| `usp_SubmitSalesDailyActivities` | `FBB818425BA37003CFC449A28D39555603D4445038FD857103F2D3483AD68EB9` | 28066 | `18B9D5854ED396CCDD2CCC94AD80E6904FA1891C9DF3F907BD94D312F1A472BF` |
| `usp_MarkRequestReadyForPayment` | `722E58AE836E9A7D4C25BAF33270A8461A065D0402985E58229B55E46C692B48` | 11179 | `118A8333EC49E8C11AAA2BCE46F4490A3DE7D11FD9EC84E01D6990DA8EBB4274` |
| `usp_RecalculateApprovalRequest` | `4E42FE742D03A34969A3529CCA92DAB901D55B5AB3467756D02C783F3D09DE34` | 7557 | `3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F` |

Verified rollback preimage already embedded:

| Procedure | Catalog SHA-256 | LEN |
| --- | --- | --- |
| `usp_MarkRequestReadyForPayment` before apply | `8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158` | 2882 |

Missing rollback preimages: `usp_UpdateSalesDailyExpenseStatus`, `usp_ProcessApprovalAction`, `usp_SubmitSalesDailyActivities`, and `usp_RecalculateApprovalRequest`. Do not substitute git, LIVE, or `Database/Deployments/RC014B`.

## Execution order

UAT and LIVE are separate. Do not reuse a UAT hash, backup, or test result as production evidence.

| Step | UAT | LIVE |
| --- | --- | --- |
| 1 | `A_Preflight_ReadOnly_UAT.sql` with Results to File. Save the five definitions and hashes. | Do not run this package. |
| 2 | New undamaged full backup of `AnmolDristiDev` finishing after `2026-10-12 14:02:57.583`, with checksums, then `RESTORE VERIFYONLY FROM DISK = N'<backup path>' WITH CHECKSUM`. | A later production package needs its own production preimages and backup. |
| 3 | Paste the four missing preimages into `D_Rollback` and their hashes into `B_Apply`. Set `@PreimageBodiesSaved` and `@AllFivePreimagesPresent` only after each pasted body hashes to the preflight value. | Not this folder. |
| 4 | Named owner sets `@OwnerApproved`, `@OwnerName`, `@RequiredBackupFinish`, and `@BackupVerifyOnlyPassed` on `B_Apply`. A separate named decision sets `@AppPublishAuthorizedBy`. That flag records the decision. It does not publish IIS. | Separate production authorization. |
| 5 | `B_Apply_L2R19_Finalization_UAT.sql` | No. |
| 6 | `C_PostApply_Verification_UAT.sql` | No. |
| 7 | Publish the reviewed `ClaimsApproval_v2.aspx.cs` only under the separate IIS decision. | No. |
| 8 | Existing matrix in the deployment checklist, using claims created through `SalesMonthlyReview`. | No. |
| Rollback | `D_Rollback_L2R19_Finalization_UAT.sql`, or an authorized restore of the backup from step 2. | A production rollback built from production preimages. |

Protected claims 97, 100, 111, 112, 118, 119, 121, and 122 stay unread and unchanged.

## Files

| File | What it does |
| --- | --- |
| `A_Preflight_ReadOnly_UAT.sql` | Identity, five module hashes and definitions, dependency hashes, TVP, columns, index filters, and the newest full backups. |
| `B_Apply_L2R19_Finalization_UAT.sql` | Five-procedure alter. Shipped closed. |
| `C_PostApply_Verification_UAT.sql` | Read-only comparison with the catalog hashes above. |
| `D_Rollback_L2R19_Finalization_UAT.sql` | Five-procedure restore. Shipped closed. Only the Mark Ready preimage is present. |
| `E_UAT_Evidence_Queries.sql` | Read-only before and after evidence for one application-created claim. |