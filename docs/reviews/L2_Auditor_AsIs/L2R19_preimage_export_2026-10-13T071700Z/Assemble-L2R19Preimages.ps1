$ErrorActionPreference = 'Stop'
$tools = 'C:\Users\ANMOL\.cursor\projects\d-PersonalRnD-AminrupWorks-Anmol-Digitization-AnmolDristi\agent-tools'
$evidence = 'D:\PersonalRnD\AminrupWorks\Anmol_Digitization\docs\reviews\L2_Auditor_AsIs\L2R19_preimage_export_2026-10-13T071700Z'
$rollback = 'D:\PersonalRnD\AminrupWorks\Anmol_Digitization\Database\Deployments\L2R19_UAT\D_Rollback_L2R19_Finalization_UAT.sql'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null

function Get-HexFromToolFile([string]$path) {
    $json = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    $rows = @($json.data) | Sort-Object { [int]$_.part }
    $sb = New-Object System.Text.StringBuilder
    foreach ($row in $rows) {
        $hx = [string]$row.hx
        if ($hx -notmatch '^[0-9A-F]+$') { throw "Non-hex content in $path part $($row.part)" }
        [void]$sb.Append($hx)
    }
    return $sb.ToString()
}

function Get-Sha256Hex([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    return ([System.BitConverter]::ToString($sha)).Replace('-', '')
}

$targets = @(
    @{
        Name = 'usp_UpdateSalesDailyExpenseStatus'
        Variable = 'Expense'
        Files = @('cb1241e4-f8ff-449a-b330-110185c90b5b.txt')
        Len = 8010
        DataLength = 16020
        Hash = '3DA56C797403A1CC0EBFF57CE5F7FF5705DC7E036E9B6EFC7A1F272FBC34EC9E'
    },
    @{
        Name = 'usp_ProcessApprovalAction'
        Variable = 'Process'
        Files = @('3c3a4274-4236-4410-aaf0-9127e9dfd5e1.txt', '7ee665f2-3a85-4afc-a870-b54ed0cd390e.txt')
        Len = 33635
        DataLength = 67270
        Hash = 'F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE'
    },
    @{
        Name = 'usp_SubmitSalesDailyActivities'
        Variable = 'Submit'
        Files = @('05927c49-1e75-4651-abc4-0ea973b0a897.txt', 'afd6138d-261e-4c94-88b0-0058db89ac9b.txt')
        Len = 24828
        DataLength = 49656
        Hash = '5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547'
    },
    @{
        Name = 'usp_RecalculateApprovalRequest'
        Variable = 'Recalc'
        Files = @('e2754b86-eeec-48b3-8647-df789322d969.txt')
        Len = 6940
        DataLength = 13880
        Hash = 'D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D'
    }
)

$accepted = @{}
$log = New-Object System.Text.StringBuilder
[void]$log.AppendLine('L2-R19 UAT preimage export')
[void]$log.AppendLine('Server=MUMANMOAPP01\SQLEXPRESS')
[void]$log.AppendLine('Database=AnmolDristiDev')
[void]$log.AppendLine('ProductVersion=15.0.2000.5')
[void]$log.AppendLine('Export method=OBJECT_DEFINITION converted with CONVERT(varchar(max), CONVERT(varbinary(max), definition), 2), split into 4000-character hex parts, concatenated in part order.')
[void]$log.AppendLine('Hash command=SHA256 of those decoded bytes. The bytes are the UTF-16LE module text with no BOM.')
[void]$log.AppendLine('Identity reconfirmed before export. TranCount=0.')
[void]$log.AppendLine('')

foreach ($target in $targets) {
    $hex = ''
    foreach ($file in $target.Files) {
        $hex += Get-HexFromToolFile (Join-Path $tools $file)
    }
    if (($hex.Length % 2) -ne 0) { throw "$($target.Name) hex length is odd: $($hex.Length)" }
    $bytes = New-Object byte[] ($hex.Length / 2)
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        $bytes[$i] = [Convert]::ToByte($hex.Substring($i * 2, 2), 16)
    }
    $text = [System.Text.Encoding]::Unicode.GetString($bytes)
    $sha = Get-Sha256Hex $bytes
    $ok = ($sha -eq $target.Hash -and $text.Length -eq $target.Len -and $bytes.Length -eq $target.DataLength)
    $bin = Join-Path $evidence ($target.Name + '.utf16le.bin')
    if ($ok) {
        [System.IO.File]::WriteAllBytes($bin, $bytes)
        $accepted[$target.Variable] = $text
    } else {
        $rejected = Join-Path $evidence ($target.Name + '.REJECTED.utf16le.bin')
        [System.IO.File]::WriteAllBytes($rejected, $bytes)
    }
    [void]$log.AppendLine($target.Name)
    [void]$log.AppendLine('expected_len=' + $target.Len)
    [void]$log.AppendLine('observed_len=' + $text.Length)
    [void]$log.AppendLine('expected_datalength=' + $target.DataLength)
    [void]$log.AppendLine('observed_datalength=' + $bytes.Length)
    [void]$log.AppendLine('expected_sha256=' + $target.Hash)
    [void]$log.AppendLine('observed_sha256=' + $sha)
    [void]$log.AppendLine('match=' + $ok)
    [void]$log.AppendLine('bin=' + $(if ($ok) { $bin } else { $rejected }))
    [void]$log.AppendLine('')
}

if ($accepted.Count -ne 4) {
    [System.IO.File]::WriteAllText((Join-Path $evidence 'HASH_VERIFICATION.txt'), $log.ToString(), (New-Object System.Text.UTF8Encoding $false))
    throw 'One or more exports did not match. Rollback script was not changed.'
}

$raw = [System.IO.File]::ReadAllBytes($rollback)
$encoding = New-Object System.Text.UTF8Encoding $false
if ($raw.Length -ge 3 -and $raw[0] -eq 0xEF -and $raw[1] -eq 0xBB -and $raw[2] -eq 0xBF) {
    $encoding = New-Object System.Text.UTF8Encoding $true
} elseif ($raw.Length -ge 2 -and $raw[0] -eq 0xFF -and $raw[1] -eq 0xFE) {
    $encoding = New-Object System.Text.UnicodeEncoding $false, $true
}
$textSql = $encoding.GetString($raw)
$original = $textSql

foreach ($target in $targets) {
    $body = $accepted[$target.Variable]
    $literal = $body.Replace("'", "''")
    $oldBody = "DECLARE @$($target.Variable)Preimage nvarchar(max) = NULL;"
    $newBody = "DECLARE @$($target.Variable)Preimage nvarchar(max) = N'" + $literal + "';"
    $oldHash = "DECLARE @$($target.Variable)PreimageHash varchar(64) = '';"
    $newHash = "DECLARE @$($target.Variable)PreimageHash varchar(64) = '$($target.Hash)';"
    if (-not $textSql.Contains($oldBody)) { throw "Body placeholder missing for $($target.Variable)" }
    if (-not $textSql.Contains($oldHash)) { throw "Hash placeholder missing for $($target.Variable)" }
    $textSql = $textSql.Replace($oldBody, $newBody).Replace($oldHash, $newHash)
}

$oldComment = @'
    Missing, and not invented:
      dbo.usp_UpdateSalesDailyExpenseStatus
      dbo.usp_ProcessApprovalAction
      dbo.usp_SubmitSalesDailyActivities
      dbo.usp_RecalculateApprovalRequest
        Checklist length 6940 and modify_date 2026-09-15 23:55:42.430 were
        recorded. The definition and its SHA-256 were not saved.
      Database/Deployments/RC014B contains an older Submit preimage. It is not
      the verified current UAT module and is not used here.

    @AllFivePreimagesPresent stays 0. The batch returns before any catalog
    read or ALTER. Do not paste a body from git, from LIVE, or from an older
    deployment folder unless A_Preflight shows that exact hash on AnmolDristiDev.
'@
$newComment = @'
    Verified UAT preimages pasted 2026-10-13 from OBJECT_DEFINITION on
    AnmolDristiDev. Each body was accepted only after its UTF-16LE SHA-256
    and LEN matched the preflight record:
      dbo.usp_UpdateSalesDailyExpenseStatus  3DA56C797403A1CC0EBFF57CE5F7FF5705DC7E036E9B6EFC7A1F272FBC34EC9E
      dbo.usp_ProcessApprovalAction          F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE
      dbo.usp_SubmitSalesDailyActivities     5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547
      dbo.usp_RecalculateApprovalRequest     D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D
    Evidence: docs/reviews/L2_Auditor_AsIs/L2R19_preimage_export_2026-10-13T071700Z

    @Execute, @RollbackApproved, and @AllFivePreimagesPresent stay 0.
    The batch returns before any catalog read or ALTER.
'@
if (-not $textSql.Contains($oldComment)) { throw 'Header comment block not found' }
$textSql = $textSql.Replace($oldComment, $newComment)
$textSql = $textSql.Replace(
    "PRINT N'BLOCKED | Paste only A_Preflight definitions for usp_UpdateSalesDailyExpenseStatus, usp_ProcessApprovalAction, usp_SubmitSalesDailyActivities, and usp_RecalculateApprovalRequest.';",
    "PRINT N'BLOCKED | Four UAT preimages are embedded and hash-checked. @Execute, @RollbackApproved, and @AllFivePreimagesPresent are still 0.';"
)
$textSql = $textSql.Replace(
    "PRINT N'BLOCKED | usp_RecalculateApprovalRequest still has no saved body. Checklist LEN 6940 is not a definition.';",
    "PRINT N'NOT RUN | Catalog was not read. No procedure was altered.';"
)

function Get-SqlLiteral([string]$sql, [string]$marker) {
    $start = $sql.IndexOf($marker)
    if ($start -lt 0) { throw "marker not found: $marker" }
    $i = $start + $marker.Length
    $sb = New-Object System.Text.StringBuilder
    while ($i -lt $sql.Length) {
        if ($sql[$i] -eq "'") {
            if ($i + 1 -lt $sql.Length -and $sql[$i + 1] -eq "'") {
                [void]$sb.Append("'")
                $i += 2
                continue
            }
            break
        }
        [void]$sb.Append($sql[$i])
        $i++
    }
    return $sb.ToString()
}

$checks = @(
    @{ Marker = "DECLARE @ExpensePreimage nvarchar(max) = N'"; Hash = '3DA56C797403A1CC0EBFF57CE5F7FF5705DC7E036E9B6EFC7A1F272FBC34EC9E'; Len = 8010; DataLength = 16020 },
    @{ Marker = "DECLARE @ProcessPreimage nvarchar(max) = N'"; Hash = 'F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE'; Len = 33635; DataLength = 67270 },
    @{ Marker = "DECLARE @SubmitPreimage nvarchar(max) = N'"; Hash = '5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547'; Len = 24828; DataLength = 49656 },
    @{ Marker = "DECLARE @RecalcPreimage nvarchar(max) = N'"; Hash = 'D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D'; Len = 6940; DataLength = 13880 },
    @{ Marker = "DECLARE @MarkPreimage nvarchar(max) = N'"; Hash = '8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158'; Len = 2882; DataLength = 5764 }
)
foreach ($check in $checks) {
    $body = Get-SqlLiteral $textSql $check.Marker
    $bytes = [System.Text.Encoding]::Unicode.GetBytes($body)
    $sha = Get-Sha256Hex $bytes
    if ($sha -ne $check.Hash -or $body.Length -ne $check.Len -or $bytes.Length -ne $check.DataLength) {
        throw "Embedded recheck failed for $($check.Marker) sha=$sha len=$($body.Length) bytes=$($bytes.Length)"
    }
}
if ($textSql -notmatch 'DECLARE @Execute bit = 0;') { throw 'Execute gate changed' }
if ($textSql -notmatch 'DECLARE @RollbackApproved bit = 0;') { throw 'Rollback gate changed' }
if ($textSql -notmatch 'DECLARE @AllFivePreimagesPresent bit = 0;') { throw 'Preimage-present gate changed' }

[System.IO.File]::WriteAllBytes($rollback, $encoding.GetBytes($textSql))
[void]$log.AppendLine('rollback_embedded_recheck=PASS for all five preimages')
[void]$log.AppendLine('gates=@Execute 0, @RollbackApproved 0, @AllFivePreimagesPresent 0')
[System.IO.File]::WriteAllText((Join-Path $evidence 'HASH_VERIFICATION.txt'), $log.ToString(), (New-Object System.Text.UTF8Encoding $false))
Write-Output 'ASSEMBLE OK'
